CREATE TABLE Users
(
  PKID String NOT NULL PRIMARY KEY,
  Username Text (255) NOT NULL,
  ApplicationName Text (255) NOT NULL,
  Email Text (128) NOT NULL,
  Comment Text (255),
  Password Text (128) NOT NULL,
  PasswordQuestion Text (255),
  PasswordAnswer Text (255),
  IsApproved Text (1), 
  LastActivityDate DateTime,
  LastLoginDate DateTime,
  LastPasswordChangedDate DateTime,
  CreationDate DateTime, 
  IsOnLine Text (1),
  IsLockedOut Text (1),
  LastLockedOutDate DateTime,
  FailedPasswordAttemptCount Integer,
  FailedPasswordAttemptWindowStart DateTime,
  FailedPasswordAnswerAttemptCount Integer,
  FailedPasswordAnswerAttemptWindowStart DateTime
);
