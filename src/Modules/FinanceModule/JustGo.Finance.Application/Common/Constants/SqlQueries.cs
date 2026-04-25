namespace JustGo.Finance.Application.Common.Constants
{
    public static class SqlQueries
    {
        public const string GetMerchantIdSQL = @"SELECT TOP 1 dm.Docid
                                                FROM Document dm 
                                                INNER JOIN MerchantProfile_Default mpd ON dm.DocId = mpd.DocId 
                                                LEFT JOIN MerchantProfile_Links mpl ON mpd.DocId = mpl.docid 
                                                LEFT JOIN Document d ON mpl.EntityId = d.DocId AND d.RepositoryId = 2 
                                                WHERE ( d.[SyncGuid] = @SyncGuid OR   dm.[SyncGuid] = @SyncGuid) ";

        public const string SelectDocIdBySyncGuid = @"Select DocId From Document Where SyncGuid = @SyncGuid";
        public const string SelectUserIdBySyncGuid = @"Select UserId From [User] Where UserSyncId = @SyncGuid"; 
        public const string SelectPaymentIdByDocId = @"select Paymentid from PaymentReceipts_Default Where DocId = @DocId";
        public const string GetOwnerIdSQL = @"DECLARE @OrgType VARCHAR(50) = (
                                                    SELECT Value 
                                                    FROM SystemSettings 
                                                    WHERE ItemKey = 'organisation.type'
                                                );

                                                DECLARE @ResultDocId INT;

                                                IF @OrgType = 'NGB'
                                                BEGIN
                                                    IF EXISTS (
                                                        SELECT 1 
                                                        FROM merchantprofile_default mpd
                                                        INNER JOIN Document d ON d.DocId = mpd.DocId
                                                        WHERE 
                                                            NOT EXISTS (
                                                                SELECT 1 
                                                                FROM merchantprofile_links mpl 
                                                                WHERE mpl.DocId = mpd.DocId
                                                            )
                                                            AND mpd.Name <> 'JustGo'
                                                            AND mpd.MerchantType = 'NGB'
                                                            AND d.SyncGuid = @SyncGuid
                                                    )
                                                    BEGIN
                                                        SET @ResultDocId = 0;
                                                    END
                                                END
                                                ELSE
                                                BEGIN
                                                    IF EXISTS (
                                                        SELECT 1 
                                                        FROM merchantprofile_default mpd
                                                        INNER JOIN Document d ON d.DocId = mpd.DocId
                                                        WHERE 
                                                            NOT EXISTS (
                                                                SELECT 1 
                                                                FROM merchantprofile_links mpl 
                                                                WHERE mpl.DocId = mpd.DocId
                                                            )
                                                            AND mpd.Name <> 'JustGo'
                                                            AND d.SyncGuid = @SyncGuid
                                                    )
                                                    BEGIN
                                                        SET @ResultDocId = -1;
                                                    END
                                                END

                                                IF @ResultDocId IS NULL
                                                BEGIN
                                                    SELECT TOP 1 
                                                        @ResultDocId = COALESCE(mpl.EntityId, d.DocId)
                                                    FROM Document d
                                                    LEFT JOIN MerchantProfile_Links mpl 
                                                        ON d.DocId = mpl.DocId
                                                    WHERE d.SyncGuid = @SyncGuid
                                                    ORDER BY mpl.EntityId; 
                                                END

                                                SELECT @ResultDocId AS DocId;
                                                ";
        public const string MAILQUEUE_INSERT = @"
                        declare @resolveSenderFormAddress nvarchar(max) = @Sender
                        declare @EmailType int =2;
                        declare @ReplyToEmailAddress nvarchar(500) = '';
                        declare @ReplyToName nvarchar(500) = '';
                        set @ReplyToEmailAddress = @resolveSenderFormAddress
                        --declare @OwnerId int = @Tag
                        if(@Ownerid > 0)
                        begin
	                        set @resolveSenderFormAddress = 'noreply@justgo.com'
	                        --set @Tag = @Ownerid
	                        declare @ClubemailAddress nvarchar(max)
	                        declare @ClubName nvarchar(max)
	                        select top 1 @ClubemailAddress = ClubemailAddress,@ClubName = ClubName from Clubs_Default where DocId = @Ownerid

	                        if(len(@ClubName)>0)
	                        begin
			                        set @ReplyToName = @ClubName
	                        end
                        end
                        else
                        begin

                        declare @OrgName nvarchar(max) = ''
                        select top 1 @OrgName = [Value] from SystemSettings where ItemKey = 'ORGANISATION.NAME'
                        set @ReplyToName = @OrgName

	                        if( (select [Value] from Systemsettings where ItemKey='ORGANISATION.OVERRIDE_SENDER_EMAIL_TO_NOREPLY') = 'true' )
	                        begin
		                        set @resolveSenderFormAddress = 'noreply@justgo.com'
	                        end
                        end

                        INSERT INTO [dbo].[MailQueue]
                                   ([Sender]
                                   ,[To]
                                   ,[Subject]
                                   ,[Mailbody]
                                   ,[AttachmentsPath]
                                   ,[FailCount]
                                   ,[Status]
                                   ,[CreatedDate]
                                   ,[Tag]
                                   ,MessageId
		                           ,EmailType
		                           ,ReplyToEmailAddress
		                           ,ReplyToName
                                   )
                             VALUES
                                   (@resolveSenderFormAddress,@To,@Subject,@MailBody,@AttachmentsPath,@FailCount,@Status,getdate(),@Tag,-1000,@EmailType,@ReplyToEmailAddress,@ReplyToName)

        ";
    }
}
