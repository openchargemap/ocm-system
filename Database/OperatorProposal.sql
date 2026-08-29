IF OBJECT_ID(N'dbo.OperatorProposal', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OperatorProposal
    (
        ID int IDENTITY(1,1) NOT NULL,
        SubmittedByUserID int NOT NULL,
        ReviewedByUserID int NULL,
        OperatorID int NULL,
        CountryID int NULL,
        Scope smallint NOT NULL,
        ProposalType smallint NOT NULL,
        Status smallint NOT NULL,
        ProposedData nvarchar(max) NOT NULL,
        PreviousData nvarchar(max) NULL,
        SubmitterComment nvarchar(max) NULL,
        DecisionComment nvarchar(max) NULL,
        ConfirmWebsiteMatch bit NOT NULL CONSTRAINT DF_OperatorProposal_ConfirmWebsiteMatch DEFAULT ((0)),
        DateSubmitted smalldatetime NOT NULL CONSTRAINT DF_OperatorProposal_DateSubmitted DEFAULT (getdate()),
        DateReviewed smalldatetime NULL,
        CONSTRAINT PK_OperatorProposal PRIMARY KEY CLUSTERED (ID),
        CONSTRAINT FK_OperatorProposal_SubmittedByUser FOREIGN KEY (SubmittedByUserID) REFERENCES dbo.[User](ID),
        CONSTRAINT FK_OperatorProposal_ReviewedByUser FOREIGN KEY (ReviewedByUserID) REFERENCES dbo.[User](ID),
        CONSTRAINT FK_OperatorProposal_Operator FOREIGN KEY (OperatorID) REFERENCES dbo.Operator(ID),
        CONSTRAINT FK_OperatorProposal_Country FOREIGN KEY (CountryID) REFERENCES dbo.Country(ID)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OperatorProposal_Review' AND object_id = OBJECT_ID(N'dbo.OperatorProposal'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_OperatorProposal_Review
        ON dbo.OperatorProposal (Status, Scope, CountryID, DateSubmitted DESC);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OperatorProposal_Submitter' AND object_id = OBJECT_ID(N'dbo.OperatorProposal'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_OperatorProposal_Submitter
        ON dbo.OperatorProposal (SubmittedByUserID, DateSubmitted DESC);
END
GO
