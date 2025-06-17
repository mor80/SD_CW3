CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

CREATE TABLE "Orders" (
    "Id" uuid NOT NULL,
    "UserId" text NOT NULL,
    "Amount" numeric NOT NULL,
    "Description" text NOT NULL,
    "Status" text NOT NULL,
    CONSTRAINT "PK_Orders" PRIMARY KEY ("Id")
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

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20250615101837_InitialCreate', '7.0.0');

COMMIT;

