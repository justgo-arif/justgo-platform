using JustGo.AssetManagement.Application.DTOs;
using JustGo.AssetManagement.Application.DTOs.Common;
using JustGo.AssetManagement.Application.DTOs.Common.JsonConfigDTOs;
using JustGo.AssetManagement.Application.Features.AssetRegisters.Queries.GetSingleAsset;
using JustGo.AssetManagement.Domain.Entities;
using JustGo.AssetManagement.Domain.Entities.Enums;
using JustGo.Authentication.Services.Interfaces.CustomMediator;
using JustGo.Authentication.Services.Interfaces.Persistence.Repositories.GenericRepositories;
using JustGoAPI.Shared.Helper;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.AssetRegisters.Commands.AssetValidationHelpers
{
    public class AssetValidator
    {


        private static bool DoesNotContainPunctuation(string input)
        {
            if (string.IsNullOrEmpty(input))
                return true;

            foreach (char c in input)
            {
                if (char.IsPunctuation(c))
                    return false;
            }

            return true;
        }

        public static async Task<(bool IsValid, string? Message)> Validate(IMediator mediator, AssetRegisterDTO model, AssetType assetType, string assetRegisterId, List<MapItemDTO<string, string>> groups)
        {

            if (assetType == null)
            {
                return (false, "Invalid Asset Type.") ;
            }

            var regConfig = JsonConvert.DeserializeObject<AssetRegistrationConfig>(assetType.AssetRegistrationConfig);

            var typeConfig = JsonConvert.DeserializeObject<AssetTypeConfig>(assetType.AssetTypeConfig);


            if (!typeConfig.AllowDuplicateAssetName)
            {
                var duplicate = await mediator.Send(new GetDuplicateAssetQuery()
                {
                    AssetRegisterId = assetRegisterId,
                    AssetName = model.AssetName
                });

                var checkDuplicate = duplicate != null;

                if (checkDuplicate)
                {
                    return (false, "Duplicate Asset Name.");
                }
            }

            if (!DoesNotContainPunctuation(model.AssetName))
            {
                return (false, "Asset name must not contains punctuation.");
            }

            string barcodeLabel = "Barcode";

            if(typeConfig.CoreFieldConfig.LabelConfig.Any(r => r.Field == "Barcode"))
            {
                barcodeLabel = typeConfig.CoreFieldConfig.LabelConfig.
                              First(r => r.Field == "Barcode").Label;
            }


            if(model.Barcode.Length != 0)
            {
                if (model.Barcode.Length != 15)
                {
                    return (false, $"{barcodeLabel} must be 15 characters.");
                }

                var checkDuplicate = await mediator.Send(new GetDuplicateBarcodesQuery()
                {
                    AssetRegisterId = assetRegisterId,
                    Barcode = model.Barcode
                });

                if (checkDuplicate)
                {
                    return (false, $"{barcodeLabel} already in use. Please enter a unique value.");
                }
            }

            if (!model.AssetOwners.Any() && string.IsNullOrEmpty(assetRegisterId))
            {
                return (false, "Must Add Owner.");
            }

            if (!typeConfig.AllowedMultiOwner && model.AssetOwners.Count() > 1)
            {
                return (false, "Multiple Owner Not Allowed.");
            }

            if (model.AssetOwners.Any(r => 
              !typeConfig.AssetOwnerTiers.Contains(Utilities.GetEnumText<OwnerType>(r.OwnerTypeId))
            ))
            {
                return (false, "Owner Tier Missmatched.");
            }

            var dataDict = JsonConvert.DeserializeObject<
                        Dictionary<string, dynamic>>(
                                JsonConvert.SerializeObject(model)
                               );

            foreach (var key in dataDict.Keys)
            {
                if (!regConfig.Steps.BasicDetail.Config.OptionalFields.Contains(key) &&
                    (dataDict[key] == null || dataDict[key].ToString() == "" || (dataDict[key] is IList list && list.Count == 0)))
                {
                    var fieldLabelObj = typeConfig.CoreFieldConfig.LabelConfig.FirstOrDefault(r => r.Field == "key");
                    var label = fieldLabelObj != null ? fieldLabelObj.Label : key;

                    return (false, $@"{label} is required.");
                }
            }

            foreach (var key in dataDict.Keys)
            {
                if (typeConfig.CoreFieldConfig.AllowedValuesConfig
                    .Any(r => r.Field == key &&
                       !r.Options.Contains(dataDict[key].ToString())
                    ))
                {
                    if (!regConfig.Steps.BasicDetail.Config.OptionalFields.Contains(key))
                    {
                        var fieldLabelObj = typeConfig.CoreFieldConfig.LabelConfig.FirstOrDefault(r => r.Field == "key");
                        var label = fieldLabelObj != null ? fieldLabelObj.Label : key;

                        return (false, $@"Provide valid value for {label}.");
                    }
                    else if(regConfig.Steps.BasicDetail.Config.OptionalFields.Contains(key) &&
                          !(dataDict[key] == null || dataDict[key].ToString() == "" || (dataDict[key] is IList list && list.Count == 0)))
                    {

                        var fieldLabelObj = typeConfig.CoreFieldConfig.LabelConfig.FirstOrDefault(r => r.Field == "key");
                        var label = fieldLabelObj != null ? fieldLabelObj.Label : key;

                        return (false, $@"Provide valid value for {label}.");
                    }
                     
                }
            }

            if(string.IsNullOrEmpty(assetRegisterId))
            {
                if (!groups.Any(r => typeConfig.Permission.Create.Contains(r.Value)))
                {
                    return (false, "User Does Not Has Permission To Create Asset.");
                }
            }
            else
            {
                if (!groups.Any(r => typeConfig.Permission.Update.Contains(r.Value)))
                {
                    return (false, "User Does Not Has Permission To Update Asset.");
                }
            }


            return (true, null);
        }
    }
}
