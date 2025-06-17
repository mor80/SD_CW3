CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

CREATE TABLE "Accounts" (
    "Id" uuid NOT NULL,
    "UserId" text NOT NULL,
    "Balance" numeric NOT NULL,
    CONSTRAINT "PK_Accounts" PRIMARY KEY ("Id")
);

CREATE TABLE "InboxMessages" (
    "Id" uuid NOT NULL,
    "OccurredOn" timestamp with time zone NOT NULL,
    "Type" text NOT NULL,
    "Content" text NOT NULL,
    "Processed" boolean NOT NULL,
    "ProcessedOn" timestamp with time zone NULL,
    CONSTRAINT "PK_InboxMessages" PRIMARY KEY ("Id")
);

CREATE TABLE "OutboxMessages" (
    "Id" uuid NOT NULL,
    "OccurredOn" timestamp with time zone NOT NULL,
    "Type" text NOT NULL,
    "Content" text NOT NULL,
    "Processed" boolean NOT NULL,
    "ProcessedOn" timestamp with time zone NULL,
    CONSTRAINT "PK_OutboxMessages" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_Accounts_UserId" ON "Accounts" ("UserId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250615102338_InitialCreate', '7.0.0');

COMMIT;

