USE AxivoraHMS;
GO

IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='__EFMigrationsHistory' AND xtype='U')
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END
GO

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE MigrationId = '20260325171204_InitialCreate')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20260325171204_InitialCreate', '9.0.0');

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE MigrationId = '20260330060904_AddLabColumnsCheck')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20260330060904_AddLabColumnsCheck', '9.0.0');
GO
