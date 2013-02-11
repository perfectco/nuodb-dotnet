﻿/****************************************************************************
* Copyright (c) 2012-2013, NuoDB, Inc.
* All rights reserved.
*
* Redistribution and use in source and binary forms, with or without
* modification, are permitted provided that the following conditions are met:
*
*   * Redistributions of source code must retain the above copyright
*     notice, this list of conditions and the following disclaimer.
*   * Redistributions in binary form must reproduce the above copyright
*     notice, this list of conditions and the following disclaimer in the
*     documentation and/or other materials provided with the distribution.
*   * Neither the name of NuoDB, Inc. nor the names of its contributors may
*     be used to endorse or promote products derived from this software
*     without specific prior written permission.
*
* THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
* ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
* WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
* DISCLAIMED. IN NO EVENT SHALL NUODB, INC. BE LIABLE FOR ANY DIRECT, INDIRECT,
* INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
* LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA,
* OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
* LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE
* OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
* ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
****************************************************************************/

using System.Collections.Generic;
using System.Data.Common;
using System.Data.NuoDB.EntityFramework.SqlGen;
using System.Data.Metadata.Edm;
using System.Data.Common.CommandTrees;

namespace System.Data.NuoDB.EntityFramework
{
    class NuoDbProviderServices : DbProviderServices
    {
        internal static object Instance = new NuoDbProviderServices();

        protected override DbCommandDefinition CreateDbCommandDefinition(DbProviderManifest providerManifest, Common.CommandTrees.DbCommandTree commandTree)
        {
            if (providerManifest == null)
                throw new ArgumentNullException("providerManifest");

            if (commandTree == null)
                throw new ArgumentNullException("commandTree");

            NuoDbCommand command = new NuoDbCommand();

            List<DbParameter> parameters;
            CommandType commandType;
            command.CommandText = SqlGenerator.GenerateSql(commandTree, out parameters, out commandType);
            command.CommandType = commandType;

            // Get the function (if any) implemented by the command tree since this influences our interpretation of parameters
            EdmFunction function = null;
            if (commandTree is DbFunctionCommandTree)
            {
                function = ((DbFunctionCommandTree)commandTree).EdmFunction;
            }

            foreach (KeyValuePair<string, TypeUsage> queryParameter in commandTree.Parameters)
            {
                NuoDbParameter parameter;

                // Use the corresponding function parameter TypeUsage where available (currently, the SSDL facets and 
                // type trump user-defined facets and type in the EntityCommand).
                FunctionParameter functionParameter;
                if (null != function && function.Parameters.TryGetValue(queryParameter.Key, false, out functionParameter))
                {
                    parameter = CreateSqlParameter(functionParameter.Name, functionParameter.TypeUsage, functionParameter.Mode, DBNull.Value);
                }
                else
                {
                    parameter = CreateSqlParameter(queryParameter.Key, queryParameter.Value, ParameterMode.In, DBNull.Value);
                }

                command.Parameters.Add(parameter);
            }

            // Now add parameters added as part of SQL gen (note: this feature is only safe for DML SQL gen which
            // does not support user parameters, where there is no risk of name collision)
            if (null != parameters && 0 < parameters.Count)
            {
                if (!(commandTree is DbInsertCommandTree) &&
                    !(commandTree is DbUpdateCommandTree) &&
                    !(commandTree is DbDeleteCommandTree))
                {
                    throw new InvalidOperationException("SqlGenParametersNotPermitted");
                }

                foreach (DbParameter parameter in parameters)
                {
                    command.Parameters.Add(parameter);
                }
            }

            return CreateCommandDefinition(command);
        }

        private NuoDbParameter CreateSqlParameter(string name, TypeUsage type, ParameterMode mode, object value)
        {
            NuoDbParameter result = new NuoDbParameter();
            result.ParameterName = name;
            result.Value = value;

			// .Direction
			ParameterDirection direction = MetadataHelpers.ParameterModeToParameterDirection(mode);
			if (result.Direction != direction)
			{
				result.Direction = direction;
			}

			// .Size and .SqlDbType
			// output parameters are handled differently (we need to ensure there is space for return
			// values where the user has not given a specific Size/MaxLength)
			bool isOutParam = mode != ParameterMode.In;
            /*
            int? size;
            FbDbType sqlDbType = GetSqlDbType(type, isOutParam, out size);

                        if (result.FbDbType != sqlDbType)
                        {
                            result.FbDbType = sqlDbType;
                        }
            // Note that we overwrite 'facet' parameters where either the value is different or
			// there is an output parameter.
			if (size.HasValue && (isOutParam || result.Size != size.Value))
			{
				result.Size = size.Value;
			}
            */

            // .IsNullable
			bool isNullable = MetadataHelpers.IsNullable(type);
			if (isOutParam || isNullable != result.IsNullable)
			{
				result.IsNullable = isNullable;
			}

			return result;
        }

        protected override DbProviderManifest GetDbProviderManifest(string manifestToken)
        {
            return new NuoDbProviderManifest();
        }

        protected override string GetDbProviderManifestToken(DbConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException("connection");

            if (!(connection is NuoDBConnection))
                throw new ArgumentException("Connection is not a valid NuoDBConnection");

            NuoDBConnection conn = (NuoDBConnection)connection;
            DataTable dsInfo = conn.GetSchema(DbMetaDataCollectionNames.DataSourceInformation);
            if (dsInfo.Rows.Count == 0)
                return "0";
            string version = dsInfo.Rows[0].Field<string>("DataSourceInternalProductVersion");
            return version;
        }
    }
}
