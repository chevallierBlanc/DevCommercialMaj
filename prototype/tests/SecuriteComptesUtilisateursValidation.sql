SET NOCOUNT ON;

DECLARE @Erreurs TABLE(Message NVARCHAR(400));
DECLARE @NbNull INT;

IF OBJECT_ID('dbo.Utilisateurs', 'U') IS NULL
BEGIN
    INSERT INTO @Erreurs VALUES (N'Table dbo.Utilisateurs introuvable.');
END
ELSE
BEGIN
    IF COL_LENGTH('dbo.Utilisateurs', 'NombreTentativesEchouees') IS NULL INSERT INTO @Erreurs VALUES (N'Colonne NombreTentativesEchouees absente.');
    IF COL_LENGTH('dbo.Utilisateurs', 'EstVerrouille') IS NULL INSERT INTO @Erreurs VALUES (N'Colonne EstVerrouille absente.');
    IF COL_LENGTH('dbo.Utilisateurs', 'DateVerrouillage') IS NULL INSERT INTO @Erreurs VALUES (N'Colonne DateVerrouillage absente.');
    IF COL_LENGTH('dbo.Utilisateurs', 'ResetPasswordHash') IS NULL INSERT INTO @Erreurs VALUES (N'Colonne ResetPasswordHash absente.');
    IF COL_LENGTH('dbo.Utilisateurs', 'ResetPasswordSel') IS NULL INSERT INTO @Erreurs VALUES (N'Colonne ResetPasswordSel absente.');
    IF COL_LENGTH('dbo.Utilisateurs', 'ResetPasswordExpireAt') IS NULL INSERT INTO @Erreurs VALUES (N'Colonne ResetPasswordExpireAt absente.');
    IF COL_LENGTH('dbo.Utilisateurs', 'ResetPasswordUsedAt') IS NULL INSERT INTO @Erreurs VALUES (N'Colonne ResetPasswordUsedAt absente.');
    IF COL_LENGTH('dbo.Utilisateurs', 'DoitChangerMotDePasse') IS NULL INSERT INTO @Erreurs VALUES (N'Colonne DoitChangerMotDePasse absente.');

    IF COL_LENGTH('dbo.Utilisateurs', 'NombreTentativesEchouees') IS NOT NULL
    BEGIN
        SET @NbNull = 0;
        EXEC sp_executesql
            N'SELECT @N = COUNT(*) FROM dbo.Utilisateurs WHERE NombreTentativesEchouees IS NULL;',
            N'@N INT OUTPUT',
            @N = @NbNull OUTPUT;
        IF @NbNull > 0 INSERT INTO @Erreurs VALUES (N'NombreTentativesEchouees contient NULL.');
    END

    IF COL_LENGTH('dbo.Utilisateurs', 'EstVerrouille') IS NOT NULL
    BEGIN
        SET @NbNull = 0;
        EXEC sp_executesql
            N'SELECT @N = COUNT(*) FROM dbo.Utilisateurs WHERE EstVerrouille IS NULL;',
            N'@N INT OUTPUT',
            @N = @NbNull OUTPUT;
        IF @NbNull > 0 INSERT INTO @Erreurs VALUES (N'EstVerrouille contient NULL.');
    END

    IF COL_LENGTH('dbo.Utilisateurs', 'DoitChangerMotDePasse') IS NOT NULL
    BEGIN
        SET @NbNull = 0;
        EXEC sp_executesql
            N'SELECT @N = COUNT(*) FROM dbo.Utilisateurs WHERE DoitChangerMotDePasse IS NULL;',
            N'@N INT OUTPUT',
            @N = @NbNull OUTPUT;
        IF @NbNull > 0 INSERT INTO @Erreurs VALUES (N'DoitChangerMotDePasse contient NULL.');
    END

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Utilisateurs_Verrouillage' AND object_id = OBJECT_ID(N'dbo.Utilisateurs'))
        INSERT INTO @Erreurs VALUES (N'Index IX_Utilisateurs_Verrouillage absent.');
END;

IF EXISTS (SELECT 1 FROM @Erreurs)
BEGIN
    SELECT 'VALIDATION_SECURITE_COMPTES_UTILISATEURS_KO' AS Resultat, Message FROM @Erreurs;
    RAISERROR('Validation securite comptes utilisateurs echouee.', 16, 1);
END
ELSE
BEGIN
    SELECT 'VALIDATION_SECURITE_COMPTES_UTILISATEURS_OK' AS Resultat;
END;
