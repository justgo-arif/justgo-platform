using System.Data;
using System.Threading;
using Dapper;
using JustGo.Authentication.Helper;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using MobileApps.Application.Features.Club.Queries.GetClubList;
using MobileApps.Application.Features.SystemSetting.Queries;
using MobileApps.Application.Features.SystemSetting.Queries.GetSystemSettings;
using MobileApps.Domain.Entities.V2;
using Newtonsoft.Json;

namespace MobileApps.Application.Features.Club.V2.Query.GetClubList
{
    class GetClubListHandler : IRequestHandler<GetClubListQuery, List<SwitcherClub>>
    {
        private readonly LazyService<IReadRepository<object>> _readRepository;
        private IMediator _mediator;
        private readonly ISystemSettingsService _systemSettingsService;
        public GetClubListHandler(LazyService<IReadRepository<object>> readRepository, IMediator mediator
            , ISystemSettingsService systemSettingsService)
        {
            _readRepository = readRepository;
            _mediator = mediator;
            _systemSettingsService = systemSettingsService;
        }
        public async Task<List<SwitcherClub>> Handle(GetClubListQuery request, CancellationToken cancellationToken)
        {
            if (!request.IsStripeMode)
            {
                var data = await GetSwitchOptions("Club", "", request.UserId, 0, request.IsClubPlusOnly);

                string jsonString = JsonConvert.SerializeObject(data);

                //Deserialize JSON string into List<SwitcherClub>
                var results = JsonConvert.DeserializeObject<List<SwitcherClub>>(jsonString);


                if (await IsUserInGroupsByUserId(request.UserId, "Admin,NGB Admin,NGB Finance"))
                {
                    var switcherClubsData = GetOrganisationInfo()
                        .Select(r => r).ToList();

                    string jsonStringClub = JsonConvert.SerializeObject(switcherClubsData);
                    //Deserialize JSON string into List<SwitcherClub>
                    var organisation = JsonConvert.DeserializeObject<List<SwitcherClub>>(jsonStringClub);


                    results = organisation.Where(r => r.DocId != -1).Concat(results).ToList();
                }
                return results;
            }
            else
            {
                var clubs = await GetSwitchOptions("Club", "", request.UserId, 0, request.IsClubPlusOnly);
                string jsonString = JsonConvert.SerializeObject(clubs);
                var results = JsonConvert.DeserializeObject<List<SwitcherClub>>(jsonString);


                var data = GetClubMerchantInfo();
                string jsonStringclubMerchants = JsonConvert.SerializeObject(data);
                var clubMerchants = JsonConvert.DeserializeObject<List<SwitcherClub>>(jsonStringclubMerchants);


                results.ForEach(x =>
                {
                    var clubMerchantGuid = clubMerchants.FirstOrDefault(c => c.DocId == x.DocId);
                    x.MerchantGuid = clubMerchantGuid == null ? "" : clubMerchantGuid.MerchantGuid;
                    x.EntityType = "Club";
                });


                if (await IsUserInGroupsByUserId(request.UserId, "Admin,NGB Admin,NGB Finance"))
                {
                    var dataNgb = GetNgbMerchantInfo();

                    string jsonStringclubNgb = JsonConvert.SerializeObject(dataNgb);


                    var ngbMerchants = JsonConvert.DeserializeObject<List<SwitcherClub>>(jsonStringclubNgb);

                    results = ngbMerchants.Concat(results).ToList();
                }
                return results;
            }
        }



        private async Task<List<IDictionary<string, object>>> GetSwitchOptions(string switchType, string allowedType, int userId, int clubDocId, bool clubPlusOnly)
        {
            var datas = new List<IDictionary<string, object>>();


            int isEventBookingArea = 0;
            int isEventManagerArea = 0;



            switch (switchType.ToLower())
            {
                case "club":
                    const string query = @"		
                    --declare @IsEventBookingArea int=0
                    declare @REGIONField nvarchar(max) = (select value from SystemSettings where ItemKey = 'CLUB.REGION_IDENTIFIER_FIELD')
                    if(len(@REGIONField)>0  and (select  top 1 s from dbo.splitstring(@REGIONField,':') where  zeroBasedOccurance = 0) = 'FM')
	                    select @REGIONField = s from dbo.splitstring(@REGIONField,':') where  zeroBasedOccurance = 1
                    else
                    BEGIN
	                    SET @REGIONField = ''
                    END

                    declare @SubREGIONField nvarchar(max) = (select value from SystemSettings where ItemKey = 'CLUB.SUB_REGION_IDENTIFIER_FIELD')
                    if(len(@SubREGIONField)>0  and (select  top 1 s from dbo.splitstring(@SubREGIONField,':') where  zeroBasedOccurance = 0) = 'FM')
	                    select @SubREGIONField = s from dbo.splitstring(@SubREGIONField,':') where  zeroBasedOccurance = 1
                    else
                    BEGIN
	                    SET @SubREGIONField = ''
                    END


                    declare @REGIONAL_ADMIN_VIEW_ALL_CLUBS bit = (select case when [Value] ='true' then 1 else 0 end from SystemSettings   where ItemKey ='ORGANISATION.REGIONAL_ADMIN_VIEW_ALL_CLUBS')
                    if not exists(select 1 from sys.tables where Name ='PermissionGrouping')
                    begin
                    CREATE TABLE [dbo].[PermissionGrouping](
	                    [Id] [int] IDENTITY(1,1) NOT NULL,
	                    [PermissionGroupName] [varchar](500) NOT NULL,
	                    [PermissionGroupId] [int] NOT NULL,
                     CONSTRAINT [PK_PermissionGrouping] PRIMARY KEY CLUSTERED 
                    (
	                    [Id] ASC
                    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
                    ) ON [PRIMARY]

                    Insert into PermissionGrouping(PermissionGroupName,PermissionGroupId)
                    Select 'NGB',25
 
                    End                   


                    if(exists(select GroupId from GroupMembers where userId={3} and (GroupId=1 or GroupId in(select PermissionGroupId from PermissionGrouping where PermissionGroupName='NGB' and GroupId=25))))
                    begin

                        select doc.SyncGuid, r.* from (
						select Clubs_Default.DocId, ClubName [Name],ClubId Reference,Location [Image],ClubaddressLine1 Address1,ClubaddressLine2 Address2,ClubaddressLine3 Address3,
						Clubtown Town,Clubpostcode PostCode,ClubPhoneNumber Phone,ClubemailAddress EmailAddress,Region [County],ClubCountry Country,ClubWebsite Website,ClubType [EntityType] 
                        from Clubs_Default where 
						{1}
						 1=1
						{7}) r 
                        INNER JOIN [Document] doc
                        ON doc.DocId = r.DocId 
						order by r.[Name]
    OPTION(OPTIMIZE FOR UNKNOWN )
                    end
                    else
                    begin
							declare @LookUpId int = (select LookupId from LookUp Where Name = 'Club Role')                    

							declare @RoleField varchar(20) = (select 'Field_'+convert(varchar(10),LookUpFieldid) from LookUpFields where LookupId = @LookUpId and Name = 'Name')
							declare @AdminField varchar(20) = (select 'Field_'+convert(varchar(10),LookUpFieldid) from LookUpFields where LookupId = @LookUpId and Name = 'IsAdmin')
							
							declare @EventAcessField varchar(20) = (select 'Field_'+convert(varchar(10),LookUpFieldid) from LookUpFields where LookupId = @LookUpId and Name = 'Event Access')
							declare @EmailAcessField varchar(20) = (select 'Field_'+convert(varchar(10),LookUpFieldid) from LookUpFields where LookupId = @LookUpId and Name = 'Email Access')
                           declare @HasBookingAccessField varchar(20) = (select 'Field_'+convert(varchar(10),LookUpFieldid) from LookUpFields where LookupId = @LookUpId and Name = 'HasBookingAccess')
							
                            declare @RegionValue varchar(200)
                            if exists(select Value from SystemSettings where itemkey='ORGANISATION.REGIONAL_ENTITY_IDENTITY')
                            begin
                             set @RegionValue = (select Value from SystemSettings where itemkey='ORGANISATION.REGIONAL_ENTITY_IDENTITY')
                            end
                            else
                            begin
                             set @RegionValue ='Region'
                            end
                            
                            declare @SubRegionValue varchar(200)
                            if exists(select Value from SystemSettings where itemkey='ORGANISATION.SUB_REGIONAL_ENTITY_IDENTITY')
                            begin
                             set @SubRegionValue = (select Value from SystemSettings where itemkey='ORGANISATION.SUB_REGIONAL_ENTITY_IDENTITY')
                            end
                            else
                            begin
                             set @SubRegionValue =''
                            end

							declare @RegionalAdminGroupId int = (select GroupId from [Group] WHere Name='Regional Admin')
							declare @RegionGroupId int; 

							declare @clubSelectQuery1 nvarchar(max)			 	
						    set @clubSelectQuery1 =CAST('' as nVarChar(MAX)) +  'declare @ClubMemberTemp Table
							(
							 ClubMemberDocId int,
							 RoleName varchar(500)
							);
								declare @AllowedType varchar(100)
							insert into @ClubMemberTemp select DocId,s  from ClubMembers_Default as cd cross apply dbo.[SplitString](cd.MyRoles,'','') where DocId in
							(select DocId from ClubMembers_Links where Entityparentid = 1 and EntityId in(select docId from Members_Default where DocId in(
														select linkId from EntityLink where SourceId= {0} and LinkParentId = 1)))

                            declare @ClubMemberTempRegion Table
							(
								ClubMemberDocId int,
								RoleName varchar(500),
								ClubName nvarchar(500)
							);
                            
                            declare @ClubMemberTempSubRegion Table
							(
								ClubMemberDocId int,
								RoleName varchar(500),
								ClubName nvarchar(500)
							);

							insert into  @ClubMemberTempRegion  
							  select cmd.docid, [Role].s, cd.clubName  from Members_Default md
							  inner join Members_links ml on ml.docid = md.docid
							  inner join clubmembers_default cmd on cmd.docid = ml.entityid
							  inner join clubmembers_links cml on cml.docid = cmd.docid
							  inner join Clubs_Default cd on cd.Docid = cml.entityId 
								INNER JOIN dbo.EntityLink el ON el.LinkId = md.DocId
							  INNER JOIN [user] u ON u.userid = el.SourceId  
							Cross Apply dbo.SplitString(cmd.MyRoles,'','') as [Role]
							where  u.userid ={0} and cd.ClubType = '''+@RegionValue+'''
                            

							insert into  @ClubMemberTempSubRegion  
							  select cmd.docid, [Role].s, cd.clubName  from Members_Default md
							  inner join Members_links ml on ml.docid = md.docid
							  inner join clubmembers_default cmd on cmd.docid = ml.entityid
							  inner join clubmembers_links cml on cml.docid = cmd.docid
							  inner join Clubs_Default cd on cd.Docid = cml.entityId 
								INNER JOIN dbo.EntityLink el ON el.LinkId = md.DocId
							  INNER JOIN [user] u ON u.userid = el.SourceId  
							Cross Apply dbo.SplitString(cmd.MyRoles,'','') as [Role]
							where  u.userid ={0} and cd.ClubType = '''+@SubRegionValue+''' and '+ cast( case when len(@SubRegionValue)>0 then 1 else 0 end as nvarchar(50))+'  =1



                            Declare @tempclub_default Table(
	                                    [DocId] [int],
	                                    [Name] [nvarchar](max) NULL,
	                                    [Reference] [nvarchar](250) NULL,
	                                    [Image] [varchar](350) NOT NULL,
	                                    [Address1] [nvarchar](max) NULL,
	                                    [Address2] [nvarchar](max) NULL,
	                                    [Address3] [nvarchar](max) NULL,
	                                    [Town] [nvarchar](max) NULL,
	                                    [PostCode] [nvarchar](max) NULL,
	                                    [Phone] [nvarchar](max) NULL,
	                                    [EmailAddress] [nvarchar](max) NULL,
	                                    [County] [nvarchar](max) NULL,
	                                    [Country] [nvarchar](max) NULL,
	                                    [Website] [nvarchar](max) NULL,
	                                    [EntityType] [nvarchar](max) NULL
                                    )

                            insert into @tempclub_default 
							 select distinct DocId,ClubName [Name],ClubId Reference,Location [Image],ClubaddressLine1 Address1,ClubaddressLine2 Address2,ClubaddressLine3 Address3,
								Clubtown Town,Clubpostcode PostCode,ClubPhoneNumber Phone,ClubemailAddress EmailAddress,Region [County],ClubCountry Country,ClubWebsite Website,ClubType [EntityType] from Clubs_Default where 
							{2}
							DocId in(select DocId from Clubs_Links where Entityparentid = 3 and (Entityid in(
							select ClubMemberDocId from @ClubMemberTemp inner join lookup_'+convert(varchar(10),@LookUpId)+' cl on cl.'+ @RoleField +' = RoleName where cl.'+ @AdminField+' = ''Yes'')
							or Entityid in(
							select ClubMemberDocId from @ClubMemberTemp inner join lookup_'+convert(varchar(10),@LookUpId)+' cl on cl.'+ @RoleField +' = RoleName where cl.'+ @EventAcessField+' = ''Yes'' and  1='+ convert(varchar(10),@IsEventManagerArea) +' )     

							or Entityid in(
							select ClubMemberDocId from @ClubMemberTemp inner join lookup_'+convert(varchar(10),@LookUpId)+' cl on cl.'+ @RoleField +' = RoleName where cl.'+ @EmailAcessField+' = ''Yes'')
                            or Entityid in(
							select ClubMemberDocId from @ClubMemberTemp inner join lookup_'+convert(varchar(10),@LookUpId)+' cl on cl.'+ @RoleField +' = RoleName where cl.'+ @HasBookingAccessField+' = ''Yes'' and  1='+ convert(varchar(10),@IsEventBookingArea) +' )                   

							))

							
							{4}

                            union
							
							 select distinct DocId,ClubName [Name],ClubId Reference,Location [Image],ClubaddressLine1 Address1,ClubaddressLine2 Address2,ClubaddressLine3 Address3,
								Clubtown Town,Clubpostcode PostCode,ClubPhoneNumber Phone,ClubemailAddress EmailAddress,Region [County],ClubCountry Country,ClubWebsite Website,ClubType [EntityType] from Clubs_Default where 
							'+ 
							case when len(@regionField)>0 then 
							 ' DocId in (select distinct DocId from ExNgbClub_LargeText where fieldid =  ' + cast( @regionField as nvarchar(50))+' and value in  (select distinct ClubName from @ClubMemberTempRegion inner join lookup_'+convert(varchar(10),@LookUpId)+' cl on cl.'+ @RoleField +' = RoleName where cl.'+ @AdminField+' = ''Yes''))'
							else ' DocId in (select DocId from clubs_default where  region in (select distinct ClubName from @ClubMemberTempRegion inner join lookup_'+convert(varchar(10),@LookUpId)+' cl on cl.'+ @RoleField +' = RoleName where cl.'+ @AdminField+' = ''Yes''))' end

							+' and ' + cast( @REGIONAL_ADMIN_VIEW_ALL_CLUBS as nvarchar(50))+' =1
                            {5}
                            {4}

                            union
							
							select distinct DocId,ClubName [Name],ClubId Reference,Location [Image],ClubaddressLine1 Address1,ClubaddressLine2 Address2,ClubaddressLine3 Address3,
								Clubtown Town,Clubpostcode PostCode,ClubPhoneNumber Phone,ClubemailAddress EmailAddress,Region [County],ClubCountry Country,ClubWebsite Website,ClubType [EntityType] from Clubs_Default where 
							'+ 
							case when len(@SubREGIONField)>0 then 
							 ' DocId in (select distinct DocId from ExNgbClub_LargeText where fieldid =  ' + cast( @SubREGIONField as nvarchar(50))+' and value in  (select distinct ClubName from @ClubMemberTempSubRegion inner join lookup_'+convert(varchar(10),@LookUpId)+' cl on cl.'+ @RoleField +' = RoleName where cl.'+ @AdminField+' = ''Yes''))'
							 else  ' '+cast( case when len(@SubRegionValue)>0 then 1 else 0 end as nvarchar(50))+'  =1' end 

							+' and ' + cast( @REGIONAL_ADMIN_VIEW_ALL_CLUBS as nvarchar(50))+' =1 and '+ cast( case when len(@SubRegionValue)>0 then 1 else 0 end as nvarchar(50))+'  =1
                            {5}
                            {4}



                            union
                             select distinct DocId,ClubName [Name],ClubId Reference,Location [Image],ClubaddressLine1 Address1,ClubaddressLine2 Address2,ClubaddressLine3 Address3,
								Clubtown Town,Clubpostcode PostCode,ClubPhoneNumber Phone,ClubemailAddress EmailAddress,Region [County],ClubCountry Country,ClubWebsite Website,ClubType [EntityType] from Clubs_Default where 
							'+ 
							case when len(@regionField)>0 then 
							 ' DocId in (select distinct DocId from ExNgbClub_LargeText where fieldid =  ' + cast( @regionField as nvarchar(50))+' and value in  (select distinct ClubName from @ClubMemberTempRegion inner join lookup_'+convert(varchar(10),@LookUpId)+' cl on cl.'+ @RoleField +' = RoleName where cl.'+ @HasBookingAccessField+' = ''Yes'')) and '+convert(varchar(10),@IsEventBookingArea)+'=1'
							else ' DocId in (select DocId from clubs_default where  region in (select distinct ClubName from @ClubMemberTempRegion inner join lookup_'+convert(varchar(10),@LookUpId)+' cl on cl.'+ @RoleField +' = RoleName where cl.'+ @AdminField+' = ''Yes'')) and '+convert(varchar(10),@IsEventBookingArea)+'=1' end  

							+' and ' + cast( @REGIONAL_ADMIN_VIEW_ALL_CLUBS as nvarchar(50))+' =1
                            {5}
                            {4}
                            union

   
							
							select distinct DocId,ClubName [Name],ClubId Reference,Location [Image],ClubaddressLine1 Address1,ClubaddressLine2 Address2,ClubaddressLine3 Address3,
								Clubtown Town,Clubpostcode PostCode,ClubPhoneNumber Phone,ClubemailAddress EmailAddress,Region [County],ClubCountry Country,ClubWebsite Website,ClubType [EntityType] from Clubs_Default where 
							'+ 
							case when len(@SubREGIONField)>0 then 
							 ' DocId in (select distinct DocId from ExNgbClub_LargeText where fieldid =  ' + cast( @SubREGIONField as nvarchar(50))+' and value in  (select distinct ClubName from @ClubMemberTempSubRegion inner join lookup_'+convert(varchar(10),@LookUpId)+' cl on cl.'+ @RoleField +' = RoleName where cl.'+ @HasBookingAccessField+' = ''Yes'')) and '+convert(varchar(10),@IsEventBookingArea)+'=1 '
							 else  ' '+cast( case when len(@SubRegionValue)>0 then 1 else 0 end as nvarchar(50))+'  =1 and '+convert(varchar(10),@IsEventBookingArea)+'=1 ' end  

							+' and ' + cast( @REGIONAL_ADMIN_VIEW_ALL_CLUBS as nvarchar(50))+' =1 and '+ cast( case when len(@SubRegionValue)>0 then 1 else 0 end as nvarchar(50))+'  =1
                            {5}
                            {4}

                            select Doc.SyncGuid, r.* from (
							select cd.*,cmd.Isprimary as ''Isprimary'' from
                              Members_Default md
							  inner join Members_links ml on ml.docid = md.docid
							  inner join clubmembers_default cmd on cmd.docid = ml.entityid
							  inner join clubmembers_links cml on cml.docid = cmd.docid
							  inner join @tempclub_default cd on cd.Docid = cml.entityId 
							  INNER JOIN dbo.EntityLink el ON el.LinkId = md.DocId
							  INNER JOIN [user] u ON u.userid = el.SourceId  
							  where u.Userid={0}
							  union
							select Cd.*,'''' as Isprimary from @tempclub_default cd where docid not in (select cd.docid from
                              Members_Default md
							  inner join Members_links ml on ml.docid = md.docid
							  inner join clubmembers_default cmd on cmd.docid = ml.entityid
							  inner join clubmembers_links cml on cml.docid = cmd.docid
							  inner join @tempclub_default cd on cd.Docid = cml.entityId 
							  INNER JOIN dbo.EntityLink el ON el.LinkId = md.DocId
							  INNER JOIN [user] u ON u.userid = el.SourceId  

							  where u.Userid={0} )) r 
							  INNER JOIN [Document] Doc ON Doc.DocId = r.DocId
                              {6}
							  order by r.Isprimary desc, r.Name	asc
OPTION(OPTIMIZE FOR UNKNOWN )
                            ';
							
							Execute(@clubSelectQuery1)

											
                    end";
                    var newquery = string.Format(query, userId,
                        string.IsNullOrWhiteSpace(allowedType)
                            ? ""
                            : " ClubType in(select s from dbo.[SplitString](@AllowedType,',')) and ",
                        string.IsNullOrWhiteSpace(allowedType)
                            ? ""
                            : "  ClubType in(select s from dbo.[SplitString]('''+@AllowedType+''','','')) and ",
                        userId,
                        clubPlusOnly
                            ? ""
                            : "",
                         string.IsNullOrWhiteSpace(allowedType)
                            ? ""
                            : " and ClubType in(select s from dbo.[SplitString]('''+@AllowedType+''','',''))  ",
                         clubPlusOnly
                             ? " and r.DocId in(Select distinct entityId from GoMembershipRegistry where status=1)"
                           //  ? ""
                           : "",
                         clubPlusOnly
                             ? "and DocId in(Select distinct entityId from GoMembershipRegistry where status=1)"
                            : ""
                        );


                    var sqlParams = new DynamicParameters();
                    sqlParams.Add("@AllowedType", allowedType);
                    sqlParams.Add("@IsEventBookingArea", isEventBookingArea);
                    sqlParams.Add("@IsEventManagerArea", isEventManagerArea);


                    var reader = await _readRepository.Value.GetListAsync(newquery, sqlParams, null, "text");
                    if (reader != null)
                    {
                        var json = JsonConvert.SerializeObject(reader);
                        var dataList = JsonConvert.DeserializeObject<List<IDictionary<string, object>>>(json);

                        foreach (var column in dataList)
                        {
                            datas.Add(column);
                        }

                    }

                    break;
                //return datas;
                case "clubteam": //return datas;

                    var clubadmindocids = new List<int>();
                    const string queryclubteam1 = @"

                        if(exists(select GroupId from GroupMembers where userId={3} and GroupId in(1,25)))
                                            begin

                                            select DocId,ClubName [Name],ClubId Reference,Location [Image],ClubaddressLine1 Address1,ClubaddressLine2 Address2,ClubaddressLine3 Address3,
                                            Clubtown Town,Clubpostcode PostCode,ClubPhoneNumber Phone,ClubemailAddress EmailAddress,Region [County],ClubCountry Country,ClubWebsite Website,ClubType [EntityType] from Clubs_Default where 
                                            {1}
                                             1=1
                                            order by ClubName
											OPTION(OPTIMIZE FOR UNKNOWN )
                                            end
                        else
                        begin   
                                    declare @LookUpId int = (select LookupId from LookUp Where Name = 'Club Role')

                                    if(@LookUpId is not null)
                                    begin
                                    declare @RoleField varchar(20) = (select 'Field_'+convert(varchar(10),LookUpFieldid) from LookUpFields where LookupId = @LookUpId and Name = 'Name')
                                    declare @AdminField varchar(20) = (select 'Field_'+convert(varchar(10),LookUpFieldid) from LookUpFields where LookupId = @LookUpId and Name = 'IsAdmin')


                                    declare @clubSelectQuery varchar(MAx) = 'declare @ClubMemberTemp Table
                                    (
                                     ClubMemberDocId int,
                                     RoleName varchar(500)
                                    );
                                        declare @AllowedType varchar(100)
                                    insert into @ClubMemberTemp select DocId,s  from ClubMembers_Default as cd cross apply dbo.[SplitString](cd.MyRoles,'','') where DocId in
                                    (select DocId from ClubMembers_Links where Entityparentid = 1 and EntityId in(select docId from Members_Default where DocId in(
                                                                select linkId from EntityLink where SourceId= {0} and LinkParentId = 1)))
     

                                    select distinct DocId,ClubName [Name],ClubId Reference,Location [Image],ClubaddressLine1 Address1,ClubaddressLine2 Address2,ClubaddressLine3 Address3,Clubtown Town,Clubpostcode PostCode,ClubPhoneNumber Phone,ClubemailAddress EmailAddress,Region [County],ClubCountry Country,ClubWebsite Website,ClubType [EntityType] from Clubs_Default where 
                                    {2}
                                    DocId in(select DocId from Clubs_Links where Entityparentid = 3 and Entityid in(
                                    select ClubMemberDocId from @ClubMemberTemp inner join lookup_'+convert(varchar(10),@LookUpId)+' cl on cl.'+ @RoleField +' = RoleName where cl.'+ @AdminField+' = ''Yes'')) order by ClubName OPTION(OPTIMIZE FOR UNKNOWN )
																 
                                    ';
                                    
                                    Execute(@clubSelectQuery)

                                    End


                                    Else
                                    Begin
                                    select DocId,ClubName [Name],ClubId Reference,Location [Image],ClubaddressLine1 Address1,ClubaddressLine2 Address2,ClubaddressLine3 Address3,Clubtown Town,Clubpostcode PostCode,ClubPhoneNumber Phone,ClubemailAddress EmailAddress,Region [County],ClubCountry Country,ClubWebsite Website,ClubType [EntityType] from Clubs_Default where 
                                    {1}
                                    DocId in(select DocId from Clubs_Links where Entityparentid = 3 and Entityid in(select DocId from ClubMembers_Default 
                                    where MyRoles like '%Admin%' and DocId in(select DocId from ClubMembers_Links where Entityparentid = 1 and EntityId in(select docId from Members_Default where DocId in(
                                    select linkId from EntityLink where SourceId={0} and LinkParentId = 1))))) order by ClubName
									OPTION(OPTIMIZE FOR UNKNOWN )  																 
                                    End

                                end
                                    ";


                    var sqlParams2 = new DynamicParameters();

                    sqlParams2.Add("@AllowedType", allowedType);

                    var queryFormat = string.Format(queryclubteam1, userId, string.IsNullOrWhiteSpace(allowedType) ? "" : "ClubType in(select s from dbo.[SplitString](@AllowedType,',')) and ", string.IsNullOrWhiteSpace(allowedType) ? "" : "ClubType in(select s from dbo.[SplitString]('''+@AllowedType+''','','')) and  ", userId);

                    var clubteam = await _readRepository.Value.GetAsync(queryFormat, sqlParams2, null, "text");

                    if (clubteam != null)
                    {
                        var json = JsonConvert.SerializeObject(clubteam);
                        var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                        var expando = new Dictionary<string, object>();
                        foreach (var column in data)
                        {
                            expando.Add(column.Key, column.Value);
                            if (column.Key == "DocId")
                                clubadmindocids.Add(Convert.ToInt32(column.Value));

                        }
                        expando.Add("Role", "ClubAdmin");

                        datas.Add(expando);

                    }



                    var queryclubteam2 = @"declare @TeamLookUpId int = (select LookupId from LookUp Where Name = 'Team Role')
	                        declare @TeamRoleField varchar(20) = (select 'Field_'+convert(varchar(10),LookUpFieldid) from LookUpFields where LookupId = @TeamLookUpId and Name = 'Name')
	                        declare @TeamAdminField varchar(20) = (select 'Field_'+convert(varchar(10),LookUpFieldid) from LookUpFields where LookupId = @TeamLookUpId and Name = 'IsAdmin')
	                        declare @TeamMemberRepositoryId varchar(10)=(select RepositoryId from Repository where Name = 'Team Members')

                            declare @clubSelectQueryByTeam varchar(MAx) = 'declare @TeamMemberTemp Table
                            (
                            TeamMemberDocId int,
                            RoleName varchar(500)
                            );
                                declare @AllowedType varchar(100)
                            insert into @TeamMemberTemp select DocId,s  from TeamMembers_Default as td cross apply dbo.[SplitString](td.TeamRoles,'','') where DocId in
                            (select DocId from TeamMembers_Links where Entityparentid = 1 and EntityId in(select docId from Members_Default where DocId in(
                                                        select linkId from EntityLink where SourceId= {0} and LinkParentId = 1)))

  
                            select DocId,ClubName [Name],ClubId Reference,Location [Image],ClubaddressLine1 Address1,ClubaddressLine2 Address2,ClubaddressLine3 Address3,
                            Clubtown Town,Clubpostcode PostCode,ClubPhoneNumber Phone,ClubemailAddress EmailAddress,Region [County],ClubCountry Country,ClubWebsite Website,ClubType [EntityType] from Clubs_Default where 
                            {1}
                            DocId in( Select DocId from Clubs_Links where EntityParentId=4 and EntityId in(
                            select distinct DocId from Teams_Default where DocId in(select DocId from Teams_Links where Entityparentid ='+@TeamMemberRepositoryId+' and Entityid in(
                            select TeamMemberDocId from @TeamMemberTemp inner join lookup_'+convert(varchar(10),@TeamLookUpId)+' tl on tl.'+ @TeamRoleField +' = RoleName where tl.'+ @TeamAdminField+' = ''Yes'')))) order by ClubName
							OPTION(OPTIMIZE FOR UNKNOWN )
                             ';							  
							   
  
                            Execute(@clubSelectQueryByTeam)";




                    var sqlParams3 = new DynamicParameters();
                    sqlParams3.Add("@AllowedType", allowedType);

                    string queryFormat3 = string.Format(queryclubteam2, userId, string.IsNullOrWhiteSpace(allowedType) ? "" : "ClubType in(select s from dbo.[SplitString]('''+@AllowedType+''','','')) and  ");

                    clubteam = await _readRepository.Value.GetAsync(queryFormat3, sqlParams3, null, "text");

                    if (clubteam != null)
                    {

                        var json = JsonConvert.SerializeObject(clubteam);
                        var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                        var expando = new Dictionary<string, object>();
                        var isExists = false;
                        foreach (var column in data)
                        {
                            if (column.Key == "DocId")
                                isExists = clubadmindocids.Contains(Convert.ToInt32(column.Value));
                            expando.Add(column.Key, column.Value);
                        }
                        if (!isExists)
                        {
                            expando.Add("Role", "TeamAdmin");
                            datas.Add(expando);
                        }

                    }

                    break;


                case "team": //return datas;   
                    var clubAdminteams = @"declare @LookUpId int = (select LookupId from LookUp Where Name = 'Club Role')
                                            if(@LookUpId is not null)
                                            begin
                                            declare @RoleField varchar(20) = (select 'Field_'+convert(varchar(10),LookUpFieldid) from LookUpFields where LookupId = @LookUpId and Name = 'Name')
                                            declare @AdminField varchar(20) = (select 'Field_'+convert(varchar(10),LookUpFieldid) from LookUpFields where LookupId = @LookUpId and Name = 'IsAdmin')



                                            declare @clubSelectQuery varchar(MAx) = 'declare @ClubMemberTemp Table
                                            (
                                             ClubMemberDocId int,
                                             RoleName varchar(500)
                                            );

                                            insert into @ClubMemberTemp select DocId,s  from ClubMembers_Default as cd cross apply dbo.[SplitString](cd.MyRoles,'','') where DocId in
                                            (select DocId from ClubMembers_Links where Entityparentid = 1 and EntityId in(select docId from Members_Default where DocId in(
                                                                        select linkId from EntityLink where SourceId= {0} and LinkParentId = 1)))

                                            if(exists(select GroupId from GroupMembers where userId={2} and GroupId in(1,25)))
                                            begin
                                            Select DocId,TeamName [Name],TeamID [Reference],Location [Image],Category Address1,TeamType Address2,AgeGroup Address3,'''' Town,'''' PostCode,
                                                ContactPhone Phone,ContactEmail EmailAddress,'''' [County],'''' Country,'''' Website,'''' [EntityType]  from Teams_Default 
                                                    where DocId in(Select DocId from Teams_Links where EntityParentId=2 and EntityId in (
                                            select distinct DocId from Clubs_Default where DocId = '+convert(varchar(10),{1})+' )) order by TeamName;
                                            
                                            end

                                            else 
                                            begin
                                             Select DocId,TeamName [Name],TeamName,TeamName [FullName],TeamID [Reference],Location [Image],Category Address1,TeamType Address2,AgeGroup Address3,'''' Town,'''' PostCode,
                                                                                            ContactPhone Phone,ContactEmail EmailAddress,'''' [County],'''' Country,'''' Website,'''' [EntityType]  from Teams_Default 
                                                                                                where DocId in(Select DocId from Teams_Links where EntityParentId=2 and EntityId in (
                                                                                        select distinct DocId from Clubs_Default where DocId = '+convert(varchar(10),{1})+' and DocId in(select DocId from Clubs_Links where Entityparentid = 3 and Entityid in(
                                                                                        select ClubMemberDocId from @ClubMemberTemp inner join lookup_'+convert(varchar(10),@LookUpId)+' cl on cl.'+ @RoleField +' = RoleName where cl.'+ @AdminField+' = ''Yes'')))) order by TeamName;
                                             
                                            end                                            
                                            '
                                            Execute(@clubSelectQuery)


                                            End";


                    string sqlFormat4 = string.Format(clubAdminteams, userId, clubDocId, userId);

                    var team = await _readRepository.Value.GetAsync(sqlFormat4, null, null, "text");

                    if (team != null)
                    {
                        var json = JsonConvert.SerializeObject(team);
                        datas.Add(JsonConvert.DeserializeObject<IDictionary<string, object>>(json));
                    }
                    if (datas.Count() == 0)
                    {
                        var teamAdminTeams = @"declare @TeamLookUpId int = (select LookupId from LookUp Where Name = 'Team Role')
                                            declare @TeamRoleField varchar(20) = (select 'Field_'+convert(varchar(10),LookUpFieldid) from LookUpFields where LookupId = @TeamLookUpId and Name = 'Name')
                                            declare @TeamAdminField varchar(20) = (select 'Field_'+convert(varchar(10),LookUpFieldid) from LookUpFields where LookupId = @TeamLookUpId and Name = 'IsAdmin')
                                            declare @TeamMemberRepositoryId varchar(10)=(select RepositoryId from Repository where Name = 'Team Members')

                                            declare @clubSelectQueryByTeam varchar(MAx) = 'declare @TeamMemberTemp Table
                                            (
                                            TeamMemberDocId int,
                                            RoleName varchar(500)
                                            );
                                                declare @AllowedType varchar(100)
                                            insert into @TeamMemberTemp select DocId,s  from TeamMembers_Default as td cross apply dbo.[SplitString](td.TeamRoles,'','') where DocId in
                                            (select DocId from TeamMembers_Links where Entityparentid = 1 and EntityId in(select docId from Members_Default where DocId in(
                                                                        select linkId from EntityLink where SourceId= {0} and LinkParentId = 1)))




                                            Select td.DocId,TeamName [Name],TeamName,TeamName [FullName],TeamID [Reference],Location [Image],Category Address1,TeamType Address2,AgeGroup Address3,'''' Town,'''' PostCode,
                                             ContactPhone Phone,ContactEmail EmailAddress,'''' [County],'''' Country,'''' Website,'''' [EntityType]  from Teams_Default td
                                            inner join Clubs_Links cl on cl.entityId= td.DocId and cl.entityparentid=4
                                            where cl.docid={1} and
                                            td.DocId in(select DocId from Teams_Links where Entityparentid ='+@TeamMemberRepositoryId+' and Entityid in(
                                            select TeamMemberDocId from @TeamMemberTemp inner join lookup_'+convert(varchar(10),@TeamLookUpId)+' tl on tl.'+ @TeamRoleField +' = RoleName where tl.'+ @TeamAdminField+' = ''Yes'')) order by TeamName
											OPTION(OPTIMIZE FOR UNKNOWN )
                                            ';
																		 

                                            Execute(@clubSelectQueryByTeam)";



                        string sqlFormat5 = string.Format(teamAdminTeams, userId, clubDocId);

                        var readerList = await _readRepository.Value.GetAsync(sqlFormat5, null, null, "text");

                        if (readerList != null)
                        {
                            var jsonData = JsonConvert.SerializeObject(readerList);
                            datas.Add(JsonConvert.DeserializeObject<IDictionary<string, object>>(jsonData));
                        }

                    }


                    break;
                case "institution":
                    const string queryIns = @"

                        if(exists(select GroupId from GroupMembers where userId={3} and GroupId in(1,25)))
                                                                begin

                                                                select DocId,ClubName [Name],ClubId Reference,Location [Image],ClubaddressLine1 Address1,ClubaddressLine2 Address2,ClubaddressLine3 Address3,
                                                                Clubtown Town,Clubpostcode PostCode,ClubPhoneNumber Phone,ClubemailAddress EmailAddress,Region [County],ClubCountry Country,ClubWebsite Website,ClubType [EntityType] from Clubs_Default where 
                                                                {1}
                                                                 1=1
                                                                order by ClubName
    
                                                                end
                    else
                    begin

                                declare @LookUpId int = (select LookupId from LookUp Where Name = 'Club Role')

                                    if(@LookUpId is not null)
                                    begin
                                    declare @RoleField varchar(20) = (select 'Field_'+convert(varchar(10),LookUpFieldid) from LookUpFields where LookupId = @LookUpId and Name = 'Name')
                                    declare @AdminField varchar(20) = (select 'Field_'+convert(varchar(10),LookUpFieldid) from LookUpFields where LookupId = @LookUpId and Name = 'IsAdmin')


                                    declare @clubSelectQuery varchar(MAx) = 'declare @ClubMemberTemp Table
                                    (
                                     ClubMemberDocId int,
                                     RoleName varchar(500)
                                    );
                                    declare @AllowedType varchar(100)
                                    insert into @ClubMemberTemp select DocId,s  from ClubMembers_Default as cd cross apply dbo.[SplitString](cd.MyRoles,'','') where DocId in
                                    (select DocId from ClubMembers_Links where Entityparentid = 1 and EntityId in(select docId from Members_Default where DocId in(
                                                                select linkId from EntityLink where SourceId= {0} and LinkParentId = 1)))
     

                                    select distinct DocId,ClubName [Name],ClubId Reference,Location [Image],ClubaddressLine1 Address1,ClubaddressLine2 Address2,ClubaddressLine3 Address3,Clubtown Town,Clubpostcode PostCode,ClubPhoneNumber Phone,ClubemailAddress EmailAddress,Region [County],ClubCountry Country,ClubWebsite Website,ClubType [EntityType] from Clubs_Default where 
                                    {2}
                                    DocId in(select DocId from Clubs_Links where Entityparentid = 3 and Entityid in(
                                    select ClubMemberDocId from @ClubMemberTemp inner join lookup_'+convert(varchar(10),@LookUpId)+' cl on cl.'+ @RoleField +' = RoleName where cl.'+ @AdminField+' = ''Yes'')) order by ClubName';
                                    
                                    Execute(@clubSelectQuery)

                                    End


                                    Else
                                    Begin
                                    select DocId,ClubName [Name],ClubId Reference,Location [Image],ClubaddressLine1 Address1,ClubaddressLine2 Address2,ClubaddressLine3 Address3,Clubtown Town,Clubpostcode PostCode,ClubPhoneNumber Phone,ClubemailAddress EmailAddress,Region [County],ClubCountry Country,ClubWebsite Website,ClubType [EntityType]  from Clubs_Default where 
                                    {1}
                                    DocId in(select DocId from Clubs_Links where Entityparentid = 3 and Entityid in(select DocId from ClubMembers_Default 
                                    where MyRoles like '%Admin%' and DocId in(select DocId from ClubMembers_Links where Entityparentid = 1 and EntityId in(select docId from Members_Default where DocId in(
                                    select linkId from EntityLink where SourceId={0} and LinkParentId = 1))))) order by ClubName
                                    End
                            end
							 
									OPTION(OPTIMIZE FOR UNKNOWN )
                                            ";

                    sqlParams = new DynamicParameters();
                    sqlParams.Add("@AllowedType", allowedType);

                    var sqlFormat = string.Format(queryIns, userId, string.IsNullOrWhiteSpace(allowedType) ? "" : "ClubType in(select s from dbo.[SplitString](@AllowedType,',')) and ", string.IsNullOrWhiteSpace(allowedType) ? "" : "ClubType in(select s from dbo.[SplitString]('''+@AllowedType+''','','')) and  ", userId);


                    var institution = await _readRepository.Value.GetAsync(sqlFormat, sqlParams, null, "text");
                    if (institution != null)
                    {
                        var jsonData = JsonConvert.SerializeObject(institution);
                        datas.Add(JsonConvert.DeserializeObject<IDictionary<string, object>>(jsonData));
                    }

                    break;

            }

            return datas;
        }



        private const string ISUSERINGROUP_ByUserId = "select UserId from GroupMembers where GroupId in (Select GroupId from [Group] where Name in (select s from dbo.SplitString(@Group,','))) and UserId=@UserId";


        private async Task<bool> IsUserInGroupsByUserId(int userId, string Group)
        {

            bool exists = false;
            var sqlParams = new DynamicParameters();
            sqlParams.Add("@UserId", userId);
            sqlParams.Add("@Group", Group);

            var reader = await _readRepository.Value.GetListAsync(ISUSERINGROUP_ByUserId, sqlParams, null, "text");
            if (reader.Count() > 0) exists = true;

            return exists;
        }

        private List<IDictionary<string, object>> GetOrganisationInfo()
        {
            string sql = @"select 

                case when (select value from SystemSettings
                where Itemkey = 'organisation.type') = 'NGB' Then 0
                else -1 End DocId,
                
                case when (select value from SystemSettings
                where Itemkey = 'organisation.type') = 'NGB' Then 
                (Select top 1  d.SyncGuid SyncGuid 
                        from merchantprofile_default mpd 
                        Inner join Document d on d.docid=mpd.docid
                        WHERE mpd.docid NOT IN (SELECT mpl.docid FROM merchantprofile_links mpl)
                        AND mpd.Name != 'JustGo'  and mpd.Merchanttype = 'NGB')

                else (Select top 1  d.SyncGuid  
                        from merchantprofile_default mpd 
                        Inner join Document d on d.docid=mpd.docid
                        WHERE mpd.docid NOT IN (SELECT mpl.docid FROM merchantprofile_links mpl)
                        AND mpd.Name != 'JustGo') End SyncGuid,

                (select value from SystemSettings
                where Itemkey = 'organisation.name')
                [Name],

                (select value from SystemSettings
                where Itemkey = 'organisation.LOGO')
                [Image],

                (select value from SystemSettings
                where Itemkey = 'ORGANISATION.CONTACT_EMAIL_ADDRESS')
                Email ";


            var rows = new List<IDictionary<string, object>>();

            var reader = _readRepository.Value.GetList(sql, null, null, "text");

            if (reader.Count() != null)
            {
                var json = JsonConvert.SerializeObject(reader);
                var dataList = JsonConvert.DeserializeObject<IList<IDictionary<string, object>>>(json);

                foreach (var item in dataList)
                {
                    rows.Add(item);
                }
            }

            return rows;
        }


        private List<IDictionary<string, object>> GetClubMerchantInfo()
        {
            string sql = @"Select cd.DocId, d.syncguid MerchantGuid 
                        FROM Clubs_default cd 
                        inner join merchantprofile_links mpl on cd.DocId = mpl.entityid and mpl.entityparentid=2
                        inner join merchantprofile_default mpd on mpd.docid=mpl.docid
                        inner join Document d on d.docid=mpd.docid";


            var rows = new List<IDictionary<string, object>>();

            var reader = _readRepository.Value.GetList(sql, null, null, "text");

            if (reader != null)
            {
                var json = JsonConvert.SerializeObject(reader);
                var dataList = JsonConvert.DeserializeObject<IList<IDictionary<string, object>>>(json);
                foreach (var item in dataList)
                {
                    rows.Add(item);
                }

            }

            return rows;
        }

        private List<IDictionary<string, object>> GetNgbMerchantInfo()
        {
            string sql = @"Select mpd.Name, d.SyncGuid MerchantGuid, 'Ngb' EntityType 
                        from merchantprofile_default mpd 
                        Inner join Document d on d.docid=mpd.docid
                        WHERE mpd.docid NOT IN (SELECT mpl.docid FROM merchantprofile_links mpl)
                        AND mpd.Name != 'JustGo'";

            var rows = new List<IDictionary<string, object>>();

            var reader = _readRepository.Value.GetList(sql, null, null, "text");

            if (reader != null)
            {
                var json = JsonConvert.SerializeObject(reader);
                var dataList = JsonConvert.DeserializeObject<IList<IDictionary<string, object>>>(json);
                foreach (var item in dataList)
                {
                    rows.Add(item);
                }
            }

            return rows;
        }
        

    }
}
