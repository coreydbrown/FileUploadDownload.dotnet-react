CREATE TABLE Files
(
    Id INT IDENTITY PRIMARY KEY,
    Content VARBINARY(MAX),
    UntrustedName NVARCHAR(255),
    ContentType NVARCHAR(255),
    Note NVARCHAR(MAX),
    Size BIGINT,
    UploadDT DATETIME,
);