ALTER TABLE "Category" DROP COLUMN IF EXISTS isdeleted;
ALTER TABLE "Category" DROP COLUMN IF EXISTS createdby;
ALTER TABLE "Category" DROP COLUMN IF EXISTS modifiedby;
ALTER TABLE "Category" DROP COLUMN IF EXISTS createdon;
ALTER TABLE "Category" DROP COLUMN IF EXISTS modifiedon;

ALTER TABLE "Product" DROP COLUMN IF EXISTS isdeleted;
ALTER TABLE "Product" DROP COLUMN IF EXISTS createdby;
ALTER TABLE "Product" DROP COLUMN IF EXISTS modifiedby;
ALTER TABLE "Product" DROP COLUMN IF EXISTS createdon;
ALTER TABLE "Product" DROP COLUMN IF EXISTS modifiedon;

ALTER TABLE "Category" ADD COLUMN IF NOT EXISTS "CreatedBy" integer NOT NULL DEFAULT 1;
ALTER TABLE "Category" ADD COLUMN IF NOT EXISTS "ModifiedBy" integer NULL;
ALTER TABLE "Category" ADD COLUMN IF NOT EXISTS "CreatedOn" timestamp with time zone NOT NULL DEFAULT now();
ALTER TABLE "Category" ADD COLUMN IF NOT EXISTS "ModifiedOn" timestamp with time zone NULL;
ALTER TABLE "Category" ADD COLUMN IF NOT EXISTS "IsDeleted" boolean NOT NULL DEFAULT false;

ALTER TABLE "Product" ADD COLUMN IF NOT EXISTS "CreatedBy" integer NOT NULL DEFAULT 1;
ALTER TABLE "Product" ADD COLUMN IF NOT EXISTS "ModifiedBy" integer NULL;
ALTER TABLE "Product" ADD COLUMN IF NOT EXISTS "CreatedOn" timestamp with time zone NOT NULL DEFAULT now();
ALTER TABLE "Product" ADD COLUMN IF NOT EXISTS "ModifiedOn" timestamp with time zone NULL;
ALTER TABLE "Product" ADD COLUMN IF NOT EXISTS "IsDeleted" boolean NOT NULL DEFAULT false;

