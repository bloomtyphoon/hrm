CREATE TABLE Employees (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CompanyId INT NOT NULL,
    DepartmentId INT NULL,
    Code NVARCHAR(50) NOT NULL UNIQUE,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    DateOfBirth DATE NULL,
    Gender NVARCHAR(10) NULL,
    Email NVARCHAR(100) NULL,
    Phone NVARCHAR(20) NULL,
    HireDate DATE NOT NULL,
    TerminationDate DATE NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Employees_Companies FOREIGN KEY (CompanyId) REFERENCES Companies(Id),
    CONSTRAINT FK_Employees_Departments FOREIGN KEY (DepartmentId) REFERENCES Departments(Id)
);
