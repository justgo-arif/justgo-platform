using AuthModule.Application.Features.Users.Queries.GetUserByUserSyncId;
using AuthModule.Domain.Entities;
using Dapper;
using JustGo.Authentication.Infrastructure.Logging;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Infrastructure.CustomAuthorizations;
using JustGo.Authentication.Services.Interfaces.Infrastructure.SystemSettings;
using JustGo.Authentication.Services.Interfaces.Infrastructure.Utilities;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGo.Membership.Application.DTOs;
using Newtonsoft.Json.Linq;
using System.Data;

namespace JustGo.Membership.Application.Features.Memberships.Queries.GetLicenses
{
    public class GetLicensesHandler : IRequestHandler<GetLicensesQuery, List<MemberLicenseDto>>
    {
        private readonly IReadRepositoryFactory _readRepository;
        private readonly IUtilityService _utilityService;
        private readonly ISystemSettingsService _systemSettings;
        private readonly IMediator _mediator;
        private readonly IAuthorizationService _authorizationService;

        public GetLicensesHandler(IReadRepositoryFactory readRepository, IUtilityService utilityService, ISystemSettingsService systemSettings
            , IMediator mediator, IAuthorizationService authorizationService)
        {
            _readRepository = readRepository;
            _utilityService = utilityService;
            _systemSettings = systemSettings;
            _mediator = mediator;
            _authorizationService = authorizationService;
        }

        public async Task<List<MemberLicenseDto>> Handle(GetLicensesQuery request, CancellationToken cancellationToken)
        {
            int userId = await _utilityService.GetCurrentUserId(cancellationToken);
            var currentUserGuid = _utilityService.GetCurrentUserGuid();
            var user = await _mediator.Send(new GetUserByUserSyncIdQuery(currentUserGuid), cancellationToken);


            if (currentUserGuid != request.Id)
            {
                var groups = await _utilityService.SelectGroupByUserAsync(userId, cancellationToken);
                var group = groups.FirstOrDefault(e => e.GroupId == 25);
                if (group == null)
                {
                    int userIdForMember = user.MemberDocId.HasValue
                                          ? await GetUserIdByMemberDocIdAsync(user.MemberDocId.Value, cancellationToken)
                                          : throw new InvalidOperationException("User.MemberDocId is null.");
                    if (await SuspensionLevelCheckAsync(userIdForMember, user.SuspensionLevel, (int)EntityType.Membership, "membership", cancellationToken))
                        return new List<MemberLicenseDto>();
                }
            }
            else
            {
                if (await SuspensionLevelCheckAsync(user.Userid, user.SuspensionLevel, (int)EntityType.Membership, "", cancellationToken))
                    return new List<MemberLicenseDto>();
            }

            // Authorization check
            await _authorizationService.IsActionAllowedAsync(userId, user.MemberDocId.HasValue ? user.MemberDocId.Value : 0, "GetLicenses", cancellationToken);

            // Get licenses
            var licenses = await GetLicensesAsync(request, cancellationToken);

            return licenses;
        }

        public async Task<List<MemberLicenseDto>> GetLicensesAsync(GetLicensesQuery request, CancellationToken cancellationToken)
        {
            var filterClause = request.LicenseTypeField > 0
                ? " and ld.licencetype in (select s from dbo.[SplitString](@licenseType,',')) and isnull(ld.HideLicence,0) != 1 "
                : " and isnull(ld.HideLicence,0) != 1 ";

            var licenseTypeProjection = request.LicenseTypeField > 0 ? ", ld.LicenceType" : string.Empty;

            var sql = $@"
                  
                  DECLARE @UserSyncId UNIQUEIDENTIFIER = @UserSyncGuid;
                  DECLARE @EntityId INT;
                  SELECT @EntityId = MemberId FROM [User] WHERE UserSyncId = @UserSyncId;

                  -- Table variables
                  DECLARE @InstallmentProductDocId TABLE(ProductDocId INT, MasterLicenseDocId INT);
                  DECLARE @memberCurrentLicense TABLE(EntityLicenseDocId INT, ProductDocId INT, ExpiryDate DATETIME);
                  DECLARE @memberCurrentLicenseNotInRenewalWindow TABLE(
                      EntityLicenseDocId INT,
                      ProductDocId INT,
                      ExpiryDate DATETIME,
                      LicenseDocId INT,
                      [Classification] NVARCHAR(MAX),
                      LicenseOwner INT,
                      InRenewal BIT
                  );
                  DECLARE @UpgradeTargetEntityId TABLE(UpgradeId INT, Config NVARCHAR(MAX), InRenewal BIT);
                  DECLARE @UpgradeLicenseIdList TABLE(UpgradeId INT, LicenseDocId INT, ProductDocId INT, InRenewal BIT);
                  DECLARE @ParallelUpgradeTargetEntityId TABLE(UpgradeId INT, Config NVARCHAR(MAX));
                  DECLARE @ParallelUpgradeLicenseIdList TABLE(Id INT);
                  DECLARE @UpgradeClassificationList TABLE(UpgradeId INT, SourceClassification NVARCHAR(MAX), TargetClassification NVARCHAR(MAX));
                  DECLARE @UpgradeClassificationSourceLicenseIdList TABLE(UpgradeId INT, LicenseDocId INT, LicenseOwner INT, TargetClassification NVARCHAR(MAX), InRenewal BIT);
                  DECLARE @UpgradeClassificationLicenseIdList TABLE(UpgradeId INT, LicenseDocId INT, LicenseOwner INT, ProductDocId INT, InRenewal BIT);
                  DECLARE @RemoveMembershipList TABLE(LicenseDocId INT, ProductDocId INT);
                  
                  -- Installment Product IDs
                  INSERT INTO @InstallmentProductDocId
                  SELECT pd.DocId, pl.EntityId
                  FROM Products_Default pd
                  INNER JOIN Products_Links pl ON pd.DocId = pl.DocId
                  INNER JOIN License_Default ld ON ld.DocId = pl.EntityId
                  WHERE pd.Category IN ('InstallmentInitialPay')
                  {filterClause};
                  
                  -- Current License for Members
                  IF EXISTS (SELECT 1 FROM Document WHERE DocId = @EntityId AND RepositoryId = 1)
                  BEGIN
                      INSERT INTO @memberCurrentLicense(EntityLicenseDocId, ExpiryDate, ProductDocId)
                      SELECT DocId, EndDate, LicenceCode
                      FROM (
                          SELECT mld.DocId, mld.EndDate, mld.LicenceCode,
                                 ROW_NUMBER() OVER (PARTITION BY mld.LicenceCode ORDER BY mld.EndDate DESC) AS rn
                          FROM MembersLicense_Default mld
                          INNER JOIN MembersLicense_Links mll ON mll.DocId = mld.DocId
                          INNER JOIN Members_Links ml ON ml.DocId = mll.EntityId
                          INNER JOIN ProcessInfo pri ON pri.PrimaryDocId = mll.DocId
                          WHERE ml.DocId = @EntityId AND mll.EntityParentId = 1 AND pri.CurrentStateId = 62
                      ) q
                      WHERE rn = 1;
                  END;
                  
                  -- Current License for Clubs
                  IF EXISTS (SELECT 1 FROM Document WHERE DocId = @EntityId AND RepositoryId = 2)
                  BEGIN
                      INSERT INTO @memberCurrentLicense(EntityLicenseDocId, ExpiryDate, ProductDocId)
                      SELECT DocId, EndDate, LicenceCode
                      FROM (
                          SELECT mld.DocId, mld.EndDate, mld.LicenceCode,
                                 ROW_NUMBER() OVER (PARTITION BY mld.LicenceType, mld.LicenceCode, mld.EntityId, mld.Name ORDER BY mld.EndDate DESC) AS rn
                          FROM MembersLicense_Default mld
                          INNER JOIN MembersLicense_Links mll ON mll.DocId = mld.DocId
                          INNER JOIN Clubs_Links cl ON cl.DocId = mll.EntityId
                          INNER JOIN ProcessInfo pri ON pri.PrimaryDocId = mll.DocId
                          WHERE mll.DocId = @EntityId AND mll.EntityParentId = 2
                      ) q
                      WHERE rn = 1;
                  END;
                  
                  -- Licenses Not in Renewal Window
                  INSERT INTO @memberCurrentLicenseNotInRenewalWindow(EntityLicenseDocId, ExpiryDate, ProductDocId, LicenseDocId, [Classification], LicenseOwner, InRenewal)
                  SELECT mc.EntityLicenseDocId, mc.ExpiryDate, mc.ProductDocId, ld.DocId, ld.[Classification], ld.LicenceOwner, 0
                  FROM @memberCurrentLicense mc
                  INNER JOIN Products_Default pd ON pd.DocId = mc.ProductDocId
                  INNER JOIN Products_Links pl ON pl.DocId = pd.DocId
                  INNER JOIN License_Default ld ON ld.DocId = pl.EntityId
                      AND DATEADD(DAY, -(ISNULL(ld.RenewalWindow, 0)), mc.ExpiryDate) >= CONVERT(DATE, GETDATE());
                  
                  DECLARE @AllowUpgradeInRenewalWindow NVARCHAR(MAX) = (SELECT TOP 1 Value FROM SystemSettings WHERE ItemKey = 'ORGANISATION.UPGRADE_FEATURE_IN_RENEWAL');
                  IF (@AllowUpgradeInRenewalWindow = 'true')
                  BEGIN
                      INSERT INTO @memberCurrentLicenseNotInRenewalWindow(EntityLicenseDocId, ExpiryDate, ProductDocId, LicenseDocId, [Classification], LicenseOwner, InRenewal)
                      SELECT mc.EntityLicenseDocId, mc.ExpiryDate, mc.ProductDocId, ld.DocId, ld.[Classification], ld.LicenceOwner, 1
                      FROM @memberCurrentLicense mc
                      INNER JOIN Products_Default pd ON pd.DocId = mc.ProductDocId
                      INNER JOIN Products_Links pl ON pl.DocId = pd.DocId
                      INNER JOIN License_Default ld ON ld.DocId = pl.EntityId
                          AND CONVERT(DATE, GETDATE()) BETWEEN CONVERT(DATE, DATEADD(DAY, -(ISNULL(ld.RenewalWindow, 0)), mc.ExpiryDate)) AND CONVERT(DATE, mc.ExpiryDate);
                  END;
                  
                  -- Upgrades and Parallel Upgrades
                  INSERT INTO @UpgradeTargetEntityId(UpgradeId, Config, InRenewal)
                  SELECT up1.UpgradeId, up1.Config, mcl.InRenewal
                  FROM @memberCurrentLicenseNotInRenewalWindow mcl
                  INNER JOIN Upgrade up1 ON mcl.LicenseDocId = up1.SourceMembership AND up1.UpgradeType = 1 AND up1.[Status] = 1 AND ISNULL(up1.UpgradeContext, 1) = 1;
                  
                  INSERT INTO @UpgradeLicenseIdList(UpgradeId, LicenseDocId, ProductDocId, InRenewal)
                  SELECT UpgradeId, 
                         CAST(JSON_VALUE(Config, '$.TargetEntity') AS INT), 
                         CAST(JSON_VALUE(Config, '$.UpgradeProductId') AS INT), 
                         InRenewal
                  FROM @UpgradeTargetEntityId;
                  
                  INSERT INTO @ParallelUpgradeTargetEntityId(UpgradeId, Config)
                  SELECT up2.UpgradeId, up2.Config
                  FROM @memberCurrentLicenseNotInRenewalWindow mcl
                  INNER JOIN Upgrade up2 ON mcl.LicenseDocId = up2.SourceMembership AND up2.UpgradeType = 2 AND up2.[Status] = 1 AND ISNULL(up2.UpgradeContext, 1) = 1;
                  
                  DECLARE @ParallelUpgradeLicenseIds NVARCHAR(MAX);
                  SET @ParallelUpgradeLicenseIds = (
                      SELECT STUFF(( 
                          SELECT ',' + JSON_VALUE(Config, '$.TargetEntity') 
                          FROM @ParallelUpgradeTargetEntityId 
                          FOR XML PATH('') 
                      ), 1, 1, '')
                  );
                  
                  INSERT INTO @ParallelUpgradeLicenseIdList(Id)
                  SELECT s FROM dbo.SplitString(@ParallelUpgradeLicenseIds, ',');
                  
                  INSERT INTO @UpgradeClassificationList(UpgradeId, SourceClassification, TargetClassification)
                  SELECT ucl.UpgradeId, 
                         JSON_VALUE(ucl.Config, '$.SourceClassification'), 
                         JSON_VALUE(ucl.Config, '$.TargetClassification')
                  FROM Upgrade ucl
                  WHERE ISNULL(ucl.UpgradeContext, 1) = 2 AND ucl.UpgradeType = 1 AND ucl.[Status] = 1;
                  
                  INSERT INTO @UpgradeClassificationSourceLicenseIdList(UpgradeId, LicenseDocId, LicenseOwner, TargetClassification, InRenewal)
                  SELECT ucl.UpgradeId, mcl.LicenseDocId, mcl.LicenseOwner, ucl.TargetClassification, mcl.InRenewal
                  FROM @memberCurrentLicenseNotInRenewalWindow mcl
                  INNER JOIN @UpgradeClassificationList ucl ON ucl.SourceClassification = mcl.[Classification];
                  
                  INSERT INTO @UpgradeClassificationLicenseIdList(UpgradeId, LicenseDocId, LicenseOwner, ProductDocId, InRenewal)
                  SELECT ucsl.UpgradeId, ld.DocId, ld.LicenceOwner, pd.DocId, ucsl.InRenewal
                  FROM @UpgradeClassificationSourceLicenseIdList ucsl
                  INNER JOIN License_Default ld ON ld.[Classification] = ucsl.TargetClassification AND ucsl.LicenseOwner = ld.LicenceOwner
                  INNER JOIN License_Links ll ON ll.DocId = ld.DocId
                  INNER JOIN Products_Default pd ON pd.DocId = ll.EntityId;
                  
                  -- Remove Membership List
                  INSERT INTO @RemoveMembershipList(LicenseDocId, ProductDocId)
                  SELECT ld.DocId, pd.DocId
                  FROM License_Default ld
                  INNER JOIN @UpgradeLicenseIdList upl ON upl.LicenseDocId = ld.DocId AND upl.InRenewal = 0
                  INNER JOIN ProcessInfo p ON ld.DocId = p.PrimaryDocId
                  INNER JOIN Products_Default pd ON pd.DocId = upl.ProductDocId
                  INNER JOIN Document d ON d.DocId = pd.DocId
                  WHERE pd.Category NOT IN ('Installment', 'InstallmentInitialPay') AND pd.Isonsale = 1 AND p.CurrentStateId = 60
                  {filterClause};
                  
                  INSERT INTO @RemoveMembershipList(LicenseDocId, ProductDocId)
                  SELECT ld.DocId, pd.DocId
                  FROM License_Default ld
                  INNER JOIN @UpgradeClassificationLicenseIdList up ON up.LicenseDocId = ld.DocId AND up.InRenewal = 0
                  INNER JOIN ProcessInfo p ON ld.DocId = p.PrimaryDocId
                  INNER JOIN Products_Default pd ON pd.DocId = up.ProductDocId
                  INNER JOIN Document d ON d.DocId = pd.DocId
                  WHERE pd.Category NOT IN ('Installment', 'InstallmentInitialPay') AND pd.Isonsale = 1 AND p.CurrentStateId = 60
                  {filterClause};
                  
                  -- Final SELECT with 6 UNION blocks
                  
                  -- Block 1
                  SELECT d.Location, ld.DocId AS LicenseDocId, pd.DocId AS ProductDocId, ld.Reference, ld.Benefits, ld.[Description],
                         ld.HidePrice, ld.Sequence, ld.LicenceOwner, pd.Code, pd.Name AS ProductName, pd.[Description] AS ProductDescription,
                         pd.Category, pd.UnitPrice AS Price, pd.Currency, pd.Availablequantity, pd.Color AS ProductColor,
                         ld.RenewalWindow, ld.InactiveWindow {licenseTypeProjection} AS LicenseType, ld.ExpiryDateEndingUnit, ld.ExpiryDateEndingValue,
                         ld.HideMembershipDuration, ld.HideViewMoreAboutMembership, ld.Classification, ld.PriceOption, ld.FromPrice,
                         ld.ToPrice, ld.MembershipJourney, ld.AlternateDisplayCurrency, pd.IsSubscriptionEnabled,
                         pd.Recurringdescription, pd.RecurringMandatory, 0 AS UpgradeType, 0 AS UpgradeId,
                         Expirydatestartingtype, Expirydatestartingvalue,
                         ISNULL(ip.ProductDocId, -1) AS InstallmentInitialPayDocId, ld.licenseConfig AS LicenseConfig
                  FROM License_Default ld
                  INNER JOIN ProcessInfo p ON ld.DocId = p.PrimaryDocId
                  INNER JOIN License_Links ll ON ld.DocId = ll.DocId
                  INNER JOIN Products_Default pd ON ll.EntityId = pd.DocId
                      AND ld.DocId NOT IN (SELECT LicenseDocId FROM @RemoveMembershipList)
                      AND pd.DocId NOT IN (SELECT ProductDocId FROM @RemoveMembershipList)
                  INNER JOIN Document d ON d.DocId = pd.DocId
                  LEFT JOIN @InstallmentProductDocId ip ON ip.MasterLicenseDocId = ld.DocId
                  WHERE (ISNULL(ld.RenewalWindow, 0) = 0 OR ld.RenewalWindow <= 0 OR ISNULL(ld.Preventpurchasingbeforerenewalwindow, 0) = 0)
                    AND pd.Category NOT IN ('Installment', 'InstallmentInitialPay')
                    AND pd.Isonsale = 1 AND p.CurrentStateId = 60 AND ll.EntityParentId = 11
                  {filterClause}
                  
                  UNION
                  
                  -- Block 2
                  SELECT d.Location, ld.DocId AS LicenseDocId, pd.DocId AS ProductDocId, ld.Reference, ld.Benefits, ld.[Description],
                         ld.HidePrice, ld.Sequence, ld.LicenceOwner, pd.Code, pd.Name AS ProductName, pd.[Description] AS ProductDescription,
                         pd.Category, pd.UnitPrice AS Price, pd.Currency, pd.Availablequantity, pd.Color AS ProductColor,
                         ld.RenewalWindow, ld.InactiveWindow {licenseTypeProjection} AS LicenseType, ld.ExpiryDateEndingUnit, ld.ExpiryDateEndingValue,
                         ld.HideMembershipDuration, ld.HideViewMoreAboutMembership, ld.Classification, ld.PriceOption, ld.FromPrice,
                         ld.ToPrice, ld.MembershipJourney, ld.AlternateDisplayCurrency, pd.IsSubscriptionEnabled,
                         pd.Recurringdescription, pd.RecurringMandatory, 0 AS UpgradeType, 0 AS UpgradeId,
                         Expirydatestartingtype, Expirydatestartingvalue,
                         ISNULL(ip.ProductDocId, -1) AS InstallmentInitialPayDocId, ld.licenseConfig AS LicenseConfig
                  FROM License_Default ld
                  INNER JOIN ProcessInfo p ON ld.DocId = p.PrimaryDocId
                  INNER JOIN License_Links ll ON ld.DocId = ll.DocId
                  INNER JOIN Products_Default pd ON ll.EntityId = pd.DocId
                      AND ld.DocId NOT IN (SELECT LicenseDocId FROM @RemoveMembershipList)
                      AND pd.DocId NOT IN (SELECT ProductDocId FROM @RemoveMembershipList)
                  INNER JOIN Document d ON d.DocId = pd.DocId
                  LEFT JOIN @InstallmentProductDocId ip ON ip.MasterLicenseDocId = ld.DocId
                  WHERE pd.DocId NOT IN (SELECT ProductDocId FROM @memberCurrentLicense)
                    AND ISNULL(ld.RenewalWindow, 0) > 0
                    AND pd.Category NOT IN ('Installment', 'InstallmentInitialPay')
                    AND pd.Isonsale = 1 AND p.CurrentStateId = 60 AND ll.EntityParentId = 11
                  {filterClause}
                  
                  UNION
                  
                  -- Block 3
                  SELECT d.Location, ld.DocId AS LicenseDocId, pd.DocId AS ProductDocId, ld.Reference, ld.Benefits, ld.[Description],
                         ld.HidePrice, ld.Sequence, ld.LicenceOwner, pd.Code, pd.Name AS ProductName, pd.[Description] AS ProductDescription,
                         pd.Category, pd.UnitPrice AS Price, pd.Currency, pd.Availablequantity, pd.Color AS ProductColor,
                         ld.RenewalWindow, ld.InactiveWindow {licenseTypeProjection} AS LicenseType, ld.ExpiryDateEndingUnit, ld.ExpiryDateEndingValue,
                         ld.HideMembershipDuration, ld.HideViewMoreAboutMembership, ld.Classification, ld.PriceOption, ld.FromPrice,
                         ld.ToPrice, ld.MembershipJourney, ld.AlternateDisplayCurrency, pd.IsSubscriptionEnabled,
                         pd.Recurringdescription, pd.RecurringMandatory, 0 AS UpgradeType, 0 AS UpgradeId,
                         Expirydatestartingtype, Expirydatestartingvalue,
                         ISNULL(ip.ProductDocId, -1) AS InstallmentInitialPayDocId, ld.licenseConfig AS LicenseConfig
                  FROM License_Default ld
                  INNER JOIN ProcessInfo p ON ld.DocId = p.PrimaryDocId AND p.CurrentStateId = 60
                  INNER JOIN License_Links ll ON ld.DocId = ll.DocId
                  INNER JOIN Products_Default pd ON ll.EntityId = pd.DocId
                      AND ld.DocId NOT IN (SELECT LicenseDocId FROM @RemoveMembershipList)
                      AND pd.DocId NOT IN (SELECT ProductDocId FROM @RemoveMembershipList)
                  INNER JOIN Document d ON d.DocId = pd.DocId
                  INNER JOIN @memberCurrentLicense mcl ON pd.DocId = mcl.ProductDocId
                  LEFT JOIN @InstallmentProductDocId ip ON ip.MasterLicenseDocId = ld.DocId
                  WHERE ISNULL(ld.RenewalWindow, 0) > 0
                    AND ISNULL(ld.Preventpurchasingbeforerenewalwindow, 0) = 1
                    AND pd.Category NOT IN ('Installment', 'InstallmentInitialPay')
                    AND DATEDIFF(DAY, GETDATE(), mcl.ExpiryDate) <= ISNULL(ld.RenewalWindow, 0)
                    AND pd.Isonsale = 1 AND ll.EntityParentId = 11
                  {filterClause}
                  
                  UNION
                  
                  -- Block 4
                  SELECT d.Location, ld.DocId AS LicenseDocId, pd.DocId AS ProductDocId, ld.Reference, ld.Benefits, ld.[Description],
                           ld.HidePrice, ld.Sequence, ld.LicenceOwner, pd.Code, pd.Name AS ProductName, pd.[Description] AS ProductDescription,
                           pd.Category, pd.UnitPrice AS Price, pd.Currency, pd.Availablequantity, pd.Color AS ProductColor,
                           ld.RenewalWindow, ld.InactiveWindow {licenseTypeProjection} AS LicenseType, ld.ExpiryDateEndingUnit, ld.ExpiryDateEndingValue,
                           ld.HideMembershipDuration, ld.HideViewMoreAboutMembership, ld.Classification, ld.PriceOption, ld.FromPrice,
                           ld.ToPrice, ld.MembershipJourney, ld.AlternateDisplayCurrency, pd.IsSubscriptionEnabled,
                           pd.Recurringdescription, pd.RecurringMandatory, 1 AS UpgradeType, u.UpgradeId AS UpgradeId,
                           Expirydatestartingtype, Expirydatestartingvalue,
                           ISNULL(ip.ProductDocId, -1) AS InstallmentInitialPayDocId, ld.licenseConfig AS LicenseConfig
                    FROM License_Default ld
                    INNER JOIN @UpgradeLicenseIdList upl ON upl.LicenseDocId = ld.DocId
                    INNER JOIN Upgrade u ON u.UpgradeId = upl.UpgradeId
                    INNER JOIN ProcessInfo p ON ld.DocId = p.PrimaryDocId
                    INNER JOIN License_Links ll ON ld.DocId = ll.DocId
                    INNER JOIN Products_Default pd ON ll.EntityId = pd.DocId
                    INNER JOIN Document d ON d.DocId = pd.DocId
                    LEFT JOIN @InstallmentProductDocId ip ON ip.MasterLicenseDocId = ld.DocId
                    WHERE pd.Isonsale = 1 AND p.CurrentStateId = 60 AND ll.EntityParentId = 11
                    {filterClause}
                  
                  UNION
                  
                  -- Block 5
                  SELECT d.Location, ld.DocId AS LicenseDocId, pd.DocId AS ProductDocId, ld.Reference, ld.Benefits, ld.[Description],
                         ld.HidePrice, ld.Sequence, ld.LicenceOwner, pd.Code, pd.Name AS ProductName, pd.[Description] AS ProductDescription,
                         pd.Category, pd.UnitPrice AS Price, pd.Currency, pd.Availablequantity, pd.Color AS ProductColor,
                         ld.RenewalWindow, ld.InactiveWindow {licenseTypeProjection} AS LicenseType, ld.ExpiryDateEndingUnit, ld.ExpiryDateEndingValue,
                         ld.HideMembershipDuration, ld.HideViewMoreAboutMembership, ld.Classification, ld.PriceOption, ld.FromPrice,
                         ld.ToPrice, ld.MembershipJourney, ld.AlternateDisplayCurrency, pd.IsSubscriptionEnabled,
                         pd.Recurringdescription, pd.RecurringMandatory, 2 AS UpgradeType, up.UpgradeId AS UpgradeId,
                         Expirydatestartingtype, Expirydatestartingvalue,
                         ISNULL(ip.ProductDocId, -1) AS InstallmentInitialPayDocId, ld.licenseConfig AS LicenseConfig
                  FROM License_Default ld
                  INNER JOIN @ParallelUpgradeLicenseIdList upId ON upId.Id = ld.DocId
                  INNER JOIN Upgrade up ON up.UpgradeId = upId.Id
                  INNER JOIN ProcessInfo p ON ld.DocId = p.PrimaryDocId
                  INNER JOIN License_Links ll ON ld.DocId = ll.DocId
                  INNER JOIN Products_Default pd ON ll.EntityId = pd.DocId
                  INNER JOIN Document d ON d.DocId = pd.DocId
                  LEFT JOIN @InstallmentProductDocId ip ON ip.MasterLicenseDocId = ld.DocId
                  WHERE pd.Isonsale = 1 AND p.CurrentStateId = 60 AND ll.EntityParentId = 11
                  {filterClause}
                  
                  UNION
                  
                  -- Block 6
                  SELECT d.Location, ld.DocId AS LicenseDocId, pd.DocId AS ProductDocId, ld.Reference, ld.Benefits, ld.[Description],
                         ld.HidePrice, ld.Sequence, ld.LicenceOwner, pd.Code, pd.Name AS ProductName, pd.[Description] AS ProductDescription,
                         pd.Category, pd.UnitPrice AS Price, pd.Currency, pd.Availablequantity, pd.Color AS ProductColor,
                         ld.RenewalWindow, ld.InactiveWindow {licenseTypeProjection} AS LicenseType, ld.ExpiryDateEndingUnit, ld.ExpiryDateEndingValue,
                         ld.HideMembershipDuration, ld.HideViewMoreAboutMembership, ld.Classification, ld.PriceOption, ld.FromPrice,
                         ld.ToPrice, ld.MembershipJourney, ld.AlternateDisplayCurrency, pd.IsSubscriptionEnabled,
                         pd.Recurringdescription, pd.RecurringMandatory, 3 AS UpgradeType, up.UpgradeId AS UpgradeId,
                         Expirydatestartingtype, expirydatestartingvalue,
                         ISNULL(ip.ProductDocId, -1) AS InstallmentInitialPayDocId, ld.licenseConfig AS LicenseConfig
                  FROM License_Default ld
                  INNER JOIN @UpgradeClassificationLicenseIdList up ON up.LicenseDocId = ld.DocId
                  INNER JOIN ProcessInfo p ON ld.DocId = p.PrimaryDocId
                  INNER JOIN License_Links ll ON ld.DocId = ll.DocId
                  INNER JOIN Products_Default pd ON ll.EntityId = pd.DocId
                  INNER JOIN Document d ON d.DocId = pd.DocId
                  LEFT JOIN @InstallmentProductDocId ip ON ip.MasterLicenseDocId = ld.DocId
                  WHERE pd.Isonsale = 1 AND p.CurrentStateId = 60 AND ll.EntityParentId = 11
                  {filterClause}
                  
                  ORDER BY LicenseDocId;
            ";

            var queryParameters = new DynamicParameters();
            queryParameters.Add("@UserSyncGuid", request.Id, dbType: DbType.Guid);
            queryParameters.Add("@licenseType", request.Type);

            var memberships = await _readRepository.GetLazyRepository<MemberLicenseDto>().Value.GetListAsync(sql, cancellationToken, queryParameters, commandType: "text");
            return memberships.ToList();
        }        

        public async Task<int> GetUserIdByMemberDocIdAsync(int docId, CancellationToken cancellationToken)
        {
            const string sql = @"
            SELECT TOP (1) SourceId 
            FROM EntityLink 
            WHERE LinkId = @DocId 
            ORDER BY SourceId DESC";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@DocId", docId);
            var userId = await _readRepository.GetLazyRepository<string>().Value.GetSingleAsync(sql, cancellationToken, queryParameters, commandType: "text");
            return Convert.ToInt32(userId);
        }

        public async Task<int> GetCurrentSuspensionLevelAsync(int userID, CancellationToken cancellationToken)
        {
            const string sql = @"SELECT U.[SuspensionLevel]
                        FROM [dbo].[User] U
                        WHERE U.[UserId] = @userID";
            var queryParameters = new DynamicParameters();
            queryParameters.Add("@userID", userID);
            var suspensionLevel = await _readRepository.GetLazyRepository<string>().Value.GetSingleAsync(sql, cancellationToken, queryParameters, commandType: "text");
            return Convert.ToInt32(suspensionLevel);
        }

        public async Task<bool> SuspensionLevelCheckAsync(int userId, int suspensionLevel, int scope, string accessArea = "", CancellationToken cancellationToken = default)
        {
            if (string.Equals(accessArea, "membership", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(accessArea, "eventbooking", StringComparison.OrdinalIgnoreCase))
            {
                if (userId > 0)
                {
                    suspensionLevel = await GetCurrentSuspensionLevelAsync(userId, cancellationToken);
                }
            }

            string? suspensionData = await _systemSettings.GetSystemSettingsByItemKey("SYSTEM.SUSPENSION_DATA_CONFIG", cancellationToken);
            if (string.IsNullOrEmpty(suspensionData))
                return false;

            try
            {
                JArray jsonArray = JArray.Parse(suspensionData);
                var scopeArray = jsonArray.OfType<JObject>()
                                          .FirstOrDefault(e => e["Value"]?.Value<int>() == suspensionLevel)?
                                          .Value<JArray>("Scope");

                if (scopeArray != null)
                {
                    return scopeArray.Any(token => token.Type == JTokenType.Integer && token.Value<int>() == scope);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing suspension data: {ex.Message}");
            }

            return false;
        }

    }
}
