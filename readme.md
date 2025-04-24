dotnet add package Npgsql
dotnet add package Dapper
dotnet add package DotNetEnv

env file:

DB_CONNECTION=Host=localhost;Port=5432;Database=taskmanagerdb;Username=your_user;Password=your_password

postgres:

CREATE TABLE Tasks (
    Id SERIAL PRIMARY KEY,
    Description TEXT NOT NULL,
    Deadline TIMESTAMP NOT NULL,
    IsCompleted BOOLEAN DEFAULT FALSE
);
