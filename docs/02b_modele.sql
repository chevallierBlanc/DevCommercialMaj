-- SQL Server 2014 - Schema initial (colonnes en francais, ASCII)

CREATE TABLE Roles (
    RoleId INT IDENTITY(1,1) PRIMARY KEY,
    NomRole NVARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE Utilisateurs (
    UtilisateurId INT IDENTITY(1,1) PRIMARY KEY,
    NomUtilisateur NVARCHAR(80) NOT NULL UNIQUE,
    MotDePasseHash VARBINARY(256) NOT NULL,
    MotDePasseSel VARBINARY(128) NOT NULL,
    EstActif BIT NOT NULL DEFAULT 1,
    CreeLe DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE UtilisateurRoles (
    UtilisateurId INT NOT NULL,
    RoleId INT NOT NULL,
    PRIMARY KEY (UtilisateurId, RoleId),
    CONSTRAINT fk_utilisateurroles_utilisateur FOREIGN KEY (UtilisateurId) REFERENCES Utilisateurs(UtilisateurId),
    CONSTRAINT fk_utilisateurroles_role FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
);

CREATE TABLE CategoriesProduits (
    CategorieId INT IDENTITY(1,1) PRIMARY KEY,
    NomCategorie NVARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE Produits (
    ProduitId INT IDENTITY(1,1) PRIMARY KEY,
    CodeBarres NVARCHAR(50) NOT NULL UNIQUE,
    Libelle NVARCHAR(200) NOT NULL,
    PrixDetail DECIMAL(18,2) NOT NULL,
    PrixGros DECIMAL(18,2) NOT NULL,
    QuantiteStock DECIMAL(18,2) NOT NULL DEFAULT 0,
    DateExpiration DATE NULL,
    CategorieId INT NULL,
    EstActif BIT NOT NULL DEFAULT 1,
    CreeLe DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ModifieLe DATETIME2 NULL,
    CONSTRAINT fk_produits_categorie FOREIGN KEY (CategorieId) REFERENCES CategoriesProduits(CategorieId),
    CONSTRAINT ck_produits_prix CHECK (PrixDetail >= 0 AND PrixGros >= 0),
    CONSTRAINT ck_produits_stock CHECK (QuantiteStock >= 0)
);

CREATE TABLE HistoriquePrixProduits (
    HistoriquePrixId INT IDENTITY(1,1) PRIMARY KEY,
    ProduitId INT NOT NULL,
    AncienPrixDetail DECIMAL(18,2) NOT NULL,
    NouveauPrixDetail DECIMAL(18,2) NOT NULL,
    AncienPrixGros DECIMAL(18,2) NOT NULL,
    NouveauPrixGros DECIMAL(18,2) NOT NULL,
    ModifiePar INT NOT NULL,
    ModifieLe DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT fk_histprix_produit FOREIGN KEY (ProduitId) REFERENCES Produits(ProduitId),
    CONSTRAINT fk_histprix_utilisateur FOREIGN KEY (ModifiePar) REFERENCES Utilisateurs(UtilisateurId)
);

CREATE TABLE Fournisseurs (
    FournisseurId INT IDENTITY(1,1) PRIMARY KEY,
    NomFournisseur NVARCHAR(200) NOT NULL,
    Telephone NVARCHAR(50) NULL,
    Email NVARCHAR(120) NULL,
    Adresse NVARCHAR(300) NULL,
    EstActif BIT NOT NULL DEFAULT 1
);

CREATE TABLE FacturesFournisseurs (
    FactureFournisseurId INT IDENTITY(1,1) PRIMARY KEY,
    FournisseurId INT NOT NULL,
    ReferenceFacture NVARCHAR(100) NOT NULL,
    DateFacture DATE NOT NULL,
    MontantTotal DECIMAL(18,2) NOT NULL,
    CreePar INT NOT NULL,
    CreeLe DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT fk_factfourn_fournisseur FOREIGN KEY (FournisseurId) REFERENCES Fournisseurs(FournisseurId),
    CONSTRAINT fk_factfourn_utilisateur FOREIGN KEY (CreePar) REFERENCES Utilisateurs(UtilisateurId),
    CONSTRAINT ck_factfourn_montant CHECK (MontantTotal >= 0)
);

CREATE TABLE Clients (
    ClientId INT IDENTITY(1,1) PRIMARY KEY,
    NomClient NVARCHAR(200) NOT NULL,
    Telephone NVARCHAR(50) NULL,
    Email NVARCHAR(120) NULL,
    Adresse NVARCHAR(300) NULL,
    LimiteCredit DECIMAL(18,2) NOT NULL DEFAULT 0,
    EstActif BIT NOT NULL DEFAULT 1
);

CREATE TABLE FacturesVente (
    FactureVenteId INT IDENTITY(1,1) PRIMARY KEY,
    NumeroFacture NVARCHAR(50) NOT NULL UNIQUE,
    ClientId INT NULL,
    SousTotal DECIMAL(18,2) NOT NULL,
    MontantRemise DECIMAL(18,2) NOT NULL DEFAULT 0,
    MontantTaxe DECIMAL(18,2) NOT NULL DEFAULT 0,
    MontantTotal DECIMAL(18,2) NOT NULL,
    Statut NVARCHAR(30) NOT NULL,
    CreePar INT NOT NULL,
    CreeLe DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ValideLe DATETIME2 NULL,
    CONSTRAINT fk_factvente_client FOREIGN KEY (ClientId) REFERENCES Clients(ClientId),
    CONSTRAINT fk_factvente_utilisateur FOREIGN KEY (CreePar) REFERENCES Utilisateurs(UtilisateurId),
    CONSTRAINT ck_factvente_montants CHECK (SousTotal >= 0 AND MontantRemise >= 0 AND MontantTaxe >= 0 AND MontantTotal >= 0),
    CONSTRAINT ck_factvente_statut CHECK (Statut IN ('EN_ATTENTE','PAYEE','ANNULEE'))
);

CREATE TABLE LignesFactureVente (
    LigneFactureVenteId INT IDENTITY(1,1) PRIMARY KEY,
    FactureVenteId INT NOT NULL,
    ProduitId INT NOT NULL,
    Quantite DECIMAL(18,2) NOT NULL,
    PrixUnitaire DECIMAL(18,2) NOT NULL,
    MontantRemise DECIMAL(18,2) NOT NULL DEFAULT 0,
    MontantLigne DECIMAL(18,2) NOT NULL,
    CONSTRAINT fk_lignefact_facture FOREIGN KEY (FactureVenteId) REFERENCES FacturesVente(FactureVenteId),
    CONSTRAINT fk_lignefact_produit FOREIGN KEY (ProduitId) REFERENCES Produits(ProduitId),
    CONSTRAINT ck_lignefact_qte CHECK (Quantite > 0),
    CONSTRAINT ck_lignefact_prix CHECK (PrixUnitaire >= 0 AND MontantRemise >= 0 AND MontantLigne >= 0)
);

CREATE TABLE Paiements (
    PaiementId INT IDENTITY(1,1) PRIMARY KEY,
    FactureVenteId INT NOT NULL,
    ModePaiement NVARCHAR(30) NOT NULL,
    ReferencePaiement NVARCHAR(100) NULL,
    Montant DECIMAL(18,2) NOT NULL,
    PayePar INT NOT NULL,
    PayeLe DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT fk_paiement_facture FOREIGN KEY (FactureVenteId) REFERENCES FacturesVente(FactureVenteId),
    CONSTRAINT fk_paiement_utilisateur FOREIGN KEY (PayePar) REFERENCES Utilisateurs(UtilisateurId),
    CONSTRAINT ck_paiement_montant CHECK (Montant > 0)
);

CREATE TABLE MouvementsStock (
    MouvementStockId INT IDENTITY(1,1) PRIMARY KEY,
    ProduitId INT NOT NULL,
    TypeMouvement NVARCHAR(30) NOT NULL,
    Quantite DECIMAL(18,2) NOT NULL,
    Reference NVARCHAR(100) NULL,
    EffectuePar INT NOT NULL,
    EffectueLe DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT fk_mouvstock_produit FOREIGN KEY (ProduitId) REFERENCES Produits(ProduitId),
    CONSTRAINT fk_mouvstock_utilisateur FOREIGN KEY (EffectuePar) REFERENCES Utilisateurs(UtilisateurId),
    CONSTRAINT ck_mouvstock_quantite CHECK (Quantite > 0)
);

CREATE TABLE JournalAudit (
    AuditId BIGINT IDENTITY(1,1) PRIMARY KEY,
    Action NVARCHAR(100) NOT NULL,
    Entite NVARCHAR(100) NOT NULL,
    EntiteId NVARCHAR(50) NOT NULL,
    Details NVARCHAR(1000) NULL,
    EffectuePar INT NOT NULL,
    EffectueLe DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT fk_audit_utilisateur FOREIGN KEY (EffectuePar) REFERENCES Utilisateurs(UtilisateurId)
);

CREATE TABLE JournalFraude (
    FraudeId BIGINT IDENTITY(1,1) PRIMARY KEY,
    Action NVARCHAR(100) NOT NULL,
    Details NVARCHAR(1000) NULL,
    EffectuePar INT NOT NULL,
    EffectueLe DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT fk_fraude_utilisateur FOREIGN KEY (EffectuePar) REFERENCES Utilisateurs(UtilisateurId)
);

CREATE INDEX ix_produits_codebarres ON Produits(CodeBarres);
CREATE INDEX ix_produits_libelle ON Produits(Libelle);
CREATE INDEX ix_factvente_statut_date ON FacturesVente(Statut, CreeLe);
CREATE INDEX ix_mouvstock_produit_date ON MouvementsStock(ProduitId, EffectueLe);

GO

-- Procedures stockees (extraits)
CREATE PROCEDURE sp_creer_facture_vente
    @NumeroFacture NVARCHAR(50),
    @ClientId INT = NULL,
    @SousTotal DECIMAL(18,2),
    @MontantRemise DECIMAL(18,2),
    @MontantTaxe DECIMAL(18,2),
    @MontantTotal DECIMAL(18,2),
    @CreePar INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO FacturesVente (NumeroFacture, ClientId, SousTotal, MontantRemise, MontantTaxe, MontantTotal, Statut, CreePar)
    VALUES (@NumeroFacture, @ClientId, @SousTotal, @MontantRemise, @MontantTaxe, @MontantTotal, 'EN_ATTENTE', @CreePar);
END
GO

CREATE PROCEDURE sp_valider_paiement
    @FactureVenteId INT,
    @ModePaiement NVARCHAR(30),
    @ReferencePaiement NVARCHAR(100) = NULL,
    @Montant DECIMAL(18,2),
    @PayePar INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRAN;

    IF EXISTS (
        SELECT 1
        FROM LignesFactureVente l
        JOIN Produits p ON p.ProduitId = l.ProduitId
        WHERE l.FactureVenteId = @FactureVenteId
        GROUP BY l.ProduitId, p.QuantiteStock
        HAVING p.QuantiteStock < SUM(l.Quantite)
    )
    BEGIN
        ROLLBACK TRAN;
        RAISERROR('Stock insuffisant pour valider le paiement.', 16, 1);
        RETURN;
    END

    INSERT INTO Paiements (FactureVenteId, ModePaiement, ReferencePaiement, Montant, PayePar)
    VALUES (@FactureVenteId, @ModePaiement, @ReferencePaiement, @Montant, @PayePar);

    -- Sortie stock selon lignes de facture
    INSERT INTO MouvementsStock (ProduitId, TypeMouvement, Quantite, Reference, EffectuePar)
    SELECT l.ProduitId, 'SORTIE', l.Quantite, CONCAT('FACTURE:', @FactureVenteId), @PayePar
    FROM LignesFactureVente l
    WHERE l.FactureVenteId = @FactureVenteId;

    UPDATE p
    SET p.QuantiteStock = p.QuantiteStock - l.Quantite,
        p.ModifieLe = SYSUTCDATETIME()
    FROM Produits p
    JOIN LignesFactureVente l ON p.ProduitId = l.ProduitId
    WHERE l.FactureVenteId = @FactureVenteId;

    UPDATE FacturesVente
    SET Statut = 'PAYEE', ValideLe = SYSUTCDATETIME()
    WHERE FactureVenteId = @FactureVenteId AND Statut = 'EN_ATTENTE';

    COMMIT TRAN;
END
GO

CREATE PROCEDURE sp_ajouter_ligne_facture
    @FactureVenteId INT,
    @ProduitId INT,
    @Quantite DECIMAL(18,2),
    @PrixUnitaire DECIMAL(18,2),
    @MontantRemise DECIMAL(18,2)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM Produits WHERE ProduitId = @ProduitId AND QuantiteStock < @Quantite)
    BEGIN
        RAISERROR('Stock insuffisant pour ce produit.', 16, 1);
        RETURN;
    END

    INSERT INTO LignesFactureVente (FactureVenteId, ProduitId, Quantite, PrixUnitaire, MontantRemise, MontantLigne)
    VALUES (@FactureVenteId, @ProduitId, @Quantite, @PrixUnitaire, @MontantRemise, (@Quantite * @PrixUnitaire) - @MontantRemise);
END
GO

CREATE PROCEDURE sp_entree_stock
    @ProduitId INT,
    @Quantite DECIMAL(18,2),
    @Reference NVARCHAR(100),
    @EffectuePar INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRAN;

    UPDATE Produits
    SET QuantiteStock = QuantiteStock + @Quantite, ModifieLe = SYSUTCDATETIME()
    WHERE ProduitId = @ProduitId;

    INSERT INTO MouvementsStock (ProduitId, TypeMouvement, Quantite, Reference, EffectuePar)
    VALUES (@ProduitId, 'ENTREE', @Quantite, @Reference, @EffectuePar);

    COMMIT TRAN;
END
GO

-- Triggers audit et anti-fraude
CREATE TRIGGER trg_produits_historique_prix
ON Produits
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO HistoriquePrixProduits (
        ProduitId, AncienPrixDetail, NouveauPrixDetail, AncienPrixGros, NouveauPrixGros, ModifiePar
    )
    SELECT
        d.ProduitId, d.PrixDetail, i.PrixDetail, d.PrixGros, i.PrixGros,
        CASE
            WHEN DATALENGTH(CONTEXT_INFO()) = 0 THEN 0
            ELSE CONVERT(INT, SUBSTRING(CONTEXT_INFO(), 1, 4))
        END
    FROM inserted i
    JOIN deleted d ON i.ProduitId = d.ProduitId
    WHERE (i.PrixDetail <> d.PrixDetail) OR (i.PrixGros <> d.PrixGros);
END
GO

CREATE TRIGGER trg_facture_interdire_modif_payee
ON FacturesVente
INSTEAD OF UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM deleted d WHERE d.Statut = 'PAYEE')
    BEGIN
        INSERT INTO JournalFraude (Action, Details, EffectuePar)
        SELECT 'MODIF_FACTURE_PAYEE', 'Tentative de modification facture payee', i.CreePar
        FROM inserted i;
        RAISERROR('Modification interdite: facture payee.', 16, 1);
        RETURN;
    END

    UPDATE fv
    SET
        NumeroFacture = i.NumeroFacture,
        ClientId = i.ClientId,
        SousTotal = i.SousTotal,
        MontantRemise = i.MontantRemise,
        MontantTaxe = i.MontantTaxe,
        MontantTotal = i.MontantTotal,
        Statut = i.Statut,
        CreePar = i.CreePar,
        CreeLe = i.CreeLe,
        ValideLe = i.ValideLe
    FROM FacturesVente fv
    JOIN inserted i ON fv.FactureVenteId = i.FactureVenteId;
END
GO

CREATE TRIGGER trg_facture_interdire_suppression_payee
ON FacturesVente
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM deleted d WHERE d.Statut = 'PAYEE')
    BEGIN
        INSERT INTO JournalFraude (Action, Details, EffectuePar)
        SELECT 'SUPPR_FACTURE_PAYEE', 'Tentative de suppression facture payee', d.CreePar
        FROM deleted d;
        RAISERROR('Suppression interdite: facture payee.', 16, 1);
        RETURN;
    END

    DELETE fv
    FROM FacturesVente fv
    JOIN deleted d ON fv.FactureVenteId = d.FactureVenteId;
END
GO

CREATE TRIGGER trg_audit_mouvements_stock
ON MouvementsStock
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO JournalAudit (Action, Entite, EntiteId, Details, EffectuePar)
    SELECT 'INSERT', 'MouvementsStock', CAST(i.MouvementStockId AS NVARCHAR(50)), 'Mouvement stock', i.EffectuePar
    FROM inserted i;
END
GO
