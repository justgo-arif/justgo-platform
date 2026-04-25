using JustGo.AssetManagement.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.AssetManagement.Application.Features.Common.Helpers
{
    public class AssetStatusHelper
    {
        public static List<AssetStatusType> getActionStatuses()
        {
            return new List<AssetStatusType>()
                    {
                        AssetStatusType.Active,
                        AssetStatusType.UnderReview,
                        AssetStatusType.PendingAction,
                    };
        }

        public static bool checkIsActionStatus(AssetStatusType status)
        {
            return getActionStatuses().Contains(status);
        }

        public static List<int> getActionStatusIds()
        {
            return new List<int>()
                    {
                        5, //AssetStatusType.Active,
                        3, //AssetStatusType.UnderReview
                        2, //AssetStatusType.PendingAction 
                    };
        }

        public static bool checkIsActionStatusId(int statusId)
        {
            return getActionStatusIds().Contains(statusId);
        }


        public static List<LeaseStatusType> getLeaseActionStatuses()
        {
            return new List<LeaseStatusType>()
                    {

                        LeaseStatusType.PendingOwnerApproval,
                        LeaseStatusType.PendingPayment,
                        LeaseStatusType.PendingConfirmation,
                        LeaseStatusType.PendingApproval,

                    };
        }

        public static bool checkIsLeaseActionStatus(LeaseStatusType status)
        {
            return getLeaseActionStatuses().Contains(status);
        }


        public static List<int> getLeaseActionStatusIds()
        {
            return new List<int>()
                    {
                        28, //LeaseStatusType.PendingOwnerApproval,
                        24, //LeaseStatusType.PendingPayment,
                        19, //LeaseStatusType.PendingConfirmation,
                        21, //LeaseStatusType.PendingApproval,
                    };
        }

        public static bool checkIsLeaseActionStatusId(int statusId)
        {
            return getLeaseActionStatusIds().Contains(statusId);
        }



        public static List<TransferStatusType> getTransferActionStatuses()
        {
            return new List<TransferStatusType>()
                    {
                        //TransferStatusType.Active, //no actionns after activation.
                        TransferStatusType.PendingOwnerApproval,
                        TransferStatusType.PendingPayment,
                        TransferStatusType.PendingConfirmation,
                        TransferStatusType.PendingApproval,

                    };
        }

        public static bool checkIsTransferActionStatus(TransferStatusType status)
        {
            return getTransferActionStatuses().Contains(status);
        }


        public static List<int> getTransferActionStatusIds()
        {
            return new List<int>()
                    { 
                        //31, //TransferStatusType.Active
                        34, //TransferStatusType.PendingOwnerApproval
                        32, //TransferStatusType.PendingPayment 
                        33, //TransferStatusType.PendingConfirmation 
                        36, //TransferStatusType.PendingApproval 
                    };
        }

        public static bool checkIsTransferActionStatusId(int statusId)
        {
            return getTransferActionStatusIds().Contains(statusId);
        }

    }
}
