-- Migration: Add Metadata JSON column to AdmissionProfile
IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[AdmissionProfile]') 
      AND name = N'Metadata'
)
BEGIN
    ALTER TABLE [dbo].[AdmissionProfile]
    ADD [Metadata] NVARCHAR(MAX) NULL;
END
GO
