using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGo.Authentication.Infrastructure.Logging
{
    public static class AuditScheme
    {
        public static class GeneralInfo //Category
        {
            public static readonly string Name = "General Info";
            public static readonly int Value = 0;
            public static class Documents //Subcategory
            {
                public static readonly string Name = "Documents";
                public static readonly int Value = 0;
                public static class Created //Action
                {
                    public static readonly string Name = "Created";
                    public static readonly int Value = 1;
                }

                public static class Deleted
                {
                    public static readonly string Name = "Deleted";
                    public static readonly int Value = 2;
                }

                public static class Updated
                {
                    public static readonly string Name = "Updated";
                    public static readonly int Value = 3;
                }

            }
            public static class SystemStatistics
            {
                public static readonly string Name = "SystemStatistics";
                public static readonly int Value = 1;
                public static class SignIn
                {
                    public static readonly string Name = "SignIn";
                    public static readonly int Value = 1;
                }

                public static class SignOut
                {
                    public static readonly string Name = "SignOut";
                    public static readonly int Value = 2;
                }

                public static class Navigation
                {
                    public static readonly string Name = "Navigation";
                    public static readonly int Value = 3;
                }

                public static class ExecuteWidgetCommand
                {
                    public static readonly string Name = "ExecuteWidgetCommand";
                    public static readonly int Value = 4;
                }

                public static class MFA
                {
                    public static readonly string Name = "MFA";
                    public static readonly int Value = 5;
                }

                public static class SignUpSuccess
                {
                    public static readonly string Name = "SignUpSuccess";
                    public static readonly int Value = 6;
                }

                public static class AttemptSignUp
                {
                    public static readonly string Name = "AttemptSignUp";
                    public static readonly int Value = 7;
                }


            }
        }

        public static class SystemError
        {
            public static readonly string Name = "System Error";
            public static readonly int Value = 1;
        }

        public static class SystemSetting
        {
            public static readonly string Name = "System Setting";
            public static readonly int Value = 2;
            public static class General
            {
                public static readonly string Name = "General";
                public static readonly int Value = 0;
            }

            public static class Optins
            {
                public static readonly string Name = "Optins";
                public static readonly int Value = 1;
                public static class NgbOptinsAdd
                {
                    public static readonly string Name = "Ngb Optins-Add";
                    public static readonly int Value = 1;
                }

                public static class NgbOptinsUpdate
                {
                    public static readonly string Name = "Ngb Optins-Update";
                    public static readonly int Value = 2;
                }

                public static class NgbOptinsDelete
                {
                    public static readonly string Name = "Ngb Optins-Delete";
                    public static readonly int Value = 3;
                }

                public static class NgbOptinsDisable
                {
                    public static readonly string Name = "Ngb Optins-Disable";
                    public static readonly int Value = 4;
                }

                public static class ClubOptinsAdd
                {
                    public static readonly string Name = "Club Optins-Add";
                    public static readonly int Value = 5;
                }

                public static class ClubOptinsUpdate
                {
                    public static readonly string Name = "Club Optins-Update";
                    public static readonly int Value = 6;
                }

                public static class ClubOptinsDelete
                {
                    public static readonly string Name = "Club Optins-Delete";
                    public static readonly int Value = 7;
                }

                public static class ClubOptinsDisable
                {
                    public static readonly string Name = "Club Optins-Disable";
                    public static readonly int Value = 8;
                }

                public static class GoMembershipOptinsAdd
                {
                    public static readonly string Name = "GoMembership Optins-Add";
                    public static readonly int Value = 9;
                }

                public static class GoMembershipOptinsUpdate
                {
                    public static readonly string Name = "GoMembership Optins-Update";
                    public static readonly int Value = 10;
                }

                public static class GoMembershipOptinsDelete
                {
                    public static readonly string Name = "GoMembership Optins-Delete";
                    public static readonly int Value = 11;
                }

                public static class GoMembershipOptinsDisable
                {
                    public static readonly string Name = "GoMembership Optins-Disable";
                    public static readonly int Value = 12;
                }

            }

            public static class Settings
            {
                public static readonly string Name = "Settings";
                public static readonly int Value = 2;
                public static class NgbSettingsUpdate
                {
                    public static readonly string Name = "Ngb Settings-Update";
                    public static readonly int Value = 1;
                }

                public static class ClubSettingsUpdate
                {
                    public static readonly string Name = "Club Settings-Update";
                    public static readonly int Value = 2;
                }

                public static class GoMembershipSettingsUpdate
                {
                    public static readonly string Name = "GoMembership Settings-Update";
                    public static readonly int Value = 3;
                }

                public static class UserSettingsUpdate
                {
                    public static readonly string Name = "User Settings-Update";
                    public static readonly int Value = 4;
                }

            }

            public static class Eula
            {
                public static readonly string Name = "Eula";
                public static readonly int Value = 3;
                public static class NgbEulaAdd
                {
                    public static readonly string Name = "Ngb Eula-Add";
                    public static readonly int Value = 1;
                }

                public static class NgbEulaUpdate
                {
                    public static readonly string Name = "Ngb Eula-Update";
                    public static readonly int Value = 2;
                }

                public static class NgbEulaDelete
                {
                    public static readonly string Name = "Ngb Eula-Delete";
                    public static readonly int Value = 3;
                }

                public static class NgbEulaDisable
                {
                    public static readonly string Name = "Ngb Eula-Disable";
                    public static readonly int Value = 4;
                }

                public static class ClubEulaAdd
                {
                    public static readonly string Name = "Club Eula-Add";
                    public static readonly int Value = 5;
                }

                public static class ClubEulaUpdate
                {
                    public static readonly string Name = "Club Eula-Update";
                    public static readonly int Value = 6;
                }

                public static class ClubEulaDelete
                {
                    public static readonly string Name = "Club Eula-Delete";
                    public static readonly int Value = 7;
                }

                public static class ClubEulaDisable
                {
                    public static readonly string Name = "Club Eula-Disable";
                    public static readonly int Value = 8;
                }

                public static class GoMembershipEulaAdd
                {
                    public static readonly string Name = "GoMembership Eula-Add";
                    public static readonly int Value = 9;
                }

                public static class GoMembershipEulaUpdate
                {
                    public static readonly string Name = "GoMembership Eula-Update";
                    public static readonly int Value = 10;
                }

                public static class GoMembershipEulaDelete
                {
                    public static readonly string Name = "GoMembership Eula-Delete";
                    public static readonly int Value = 11;
                }

                public static class GoMembershipEulaDisable
                {
                    public static readonly string Name = "GoMembership Eula-Disable";
                    public static readonly int Value = 12;
                }

            }

        }

        public static class UserChanged
        {
            public static readonly string Name = "User Changed";
            public static readonly int Value = 3;
            public static class General
            {
                public static readonly string Name = "General";
                public static readonly int Value = 0;
                public static class Created
                {
                    public static readonly string Name = "Created";
                    public static readonly int Value = 1;
                }

                public static class Deleted
                {
                    public static readonly string Name = "Deleted";
                    public static readonly int Value = 2;
                }

            }

            public static class BasicDetails
            {
                public static readonly string Name = "Basic Details";
                public static readonly int Value = 1;
                public static class Profile
                {
                    public static readonly string Name = "Profile";
                    public static readonly int Value = 1;
                }

                public static class EmailChanged
                {
                    public static readonly string Name = "Email Changed";
                    public static readonly int Value = 2;
                }

                public static class DobChanged
                {
                    public static readonly string Name = "Dob Changed";
                    public static readonly int Value = 3;
                }

                public static class PictureChanged
                {
                    public static readonly string Name = "Picture Changed";
                    public static readonly int Value = 4;
                }

                public static class LoginIdChanged
                {
                    public static readonly string Name = "Login Id Changed";
                    public static readonly int Value = 5;
                }

                public static class ActiveStatusChanged
                {
                    public static readonly string Name = "Active Status Changed";
                    public static readonly int Value = 6;
                }

                public static class PasswordChanged
                {
                    public static readonly string Name = "Password Changed";
                    public static readonly int Value = 7;
                }

                public static class MergeUser
                {
                    public static readonly string Name = "Merge User";
                    public static readonly int Value = 8;
                }

                public static class GenderChanged
                {
                    public static readonly string Name = "Gender Changed";
                    public static readonly int Value = 9;
                }

                public static class PhoneNumberChanged
                {
                    public static readonly string Name = "Phone Number Changed";
                    public static readonly int Value = 10;
                }

                public static class NameChanged
                {
                    public static readonly string Name = "Name Changed";
                    public static readonly int Value = 11;
                }

                public static class AddressChanged
                {
                    public static readonly string Name = "Address Changed";
                    public static readonly int Value = 12;
                }

            }

            public static class EmergencyContact
            {
                public static readonly string Name = "Emergency Contact";
                public static readonly int Value = 2;
                public static class EmergencyContactAdd
                {
                    public static readonly string Name = "Emergency Contact-Add";
                    public static readonly int Value = 1;
                }

                public static class EmergencyContactUpdate
                {
                    public static readonly string Name = "Emergency Contact-Update";
                    public static readonly int Value = 2;
                }

                public static class EmergencyContactDelete
                {
                    public static readonly string Name = "Emergency Contact-Delete";
                    public static readonly int Value = 3;
                }

            }

            public static class AdditionalDetails
            {
                public static readonly string Name = "Additional Details";
                public static readonly int Value = 3;
            }

            public static class Optins
            {
                public static readonly string Name = "Optins";
                public static readonly int Value = 4;
                public static class Created
                {
                    public static readonly string Name = "Created";
                    public static readonly int Value = 1;
                }

                public static class Deleted
                {
                    public static readonly string Name = "Deleted";
                    public static readonly int Value = 2;
                }

                public static class Updated
                {
                    public static readonly string Name = "Updated";
                    public static readonly int Value = 3;
                }

            }

            public static class Payment
            {
                public static readonly string Name = "Payment";
                public static readonly int Value = 5;
                public static class PlanCreated
                {
                    public static readonly string Name = "Plan-Created";
                    public static readonly int Value = 1;
                }

                public static class PlanUpdate
                {
                    public static readonly string Name = "Plan-Update";
                    public static readonly int Value = 2;
                }

                public static class PlanFailed
                {
                    public static readonly string Name = "Plan-Failed";
                    public static readonly int Value = 3;
                }

                public static class PlanCancelled
                {
                    public static readonly string Name = "Plan-Cancelled";
                    public static readonly int Value = 4;
                }

                public static class CardInfoChange
                {
                    public static readonly string Name = "Card Info Change";
                    public static readonly int Value = 5;
                }

                public static class BankInfoChange
                {
                    public static readonly string Name = "Bank Info Change";
                    public static readonly int Value = 6;
                }

                public static class MandateChange
                {
                    public static readonly string Name = "Mandate Change";
                    public static readonly int Value = 7;
                }

                public static class ScheduleDateChange
                {
                    public static readonly string Name = "ScheduleDate Change";
                    public static readonly int Value = 8;
                }

                public static class ReactivateSchedule
                {
                    public static readonly string Name = "Reactivate Schedule";
                    public static readonly int Value = 9;
                }

                public static class RecurringCustomerCreated
                {
                    public static readonly string Name = "RecurringCustomer Created";
                    public static readonly int Value = 10;
                }

                public static class RecurringCustomerUpdated
                {
                    public static readonly string Name = "RecurringCustomer Updated";
                    public static readonly int Value = 11;
                }

            }

            public static class StatusChanged
            {
                public static readonly string Name = "Status Changed";
                public static readonly int Value = 6;
            }

            public static class CourseBooking
            {
                public static readonly string Name = "Course Booking";
                public static readonly int Value = 7;
            }

            public static class Qualification
            {
                public static readonly string Name = "Qualification";
                public static readonly int Value = 8;
                public static class QualificationAdd
                {
                    public static readonly string Name = "Qualification-Add";
                    public static readonly int Value = 1;
                }

                public static class QualificationExpired
                {
                    public static readonly string Name = "Qualification-Expired";
                    public static readonly int Value = 2;
                }

                public static class QualificationApproved
                {
                    public static readonly string Name = "Qualification-Approved";
                    public static readonly int Value = 3;
                }

                public static class QualificationRejected
                {
                    public static readonly string Name = "Qualification-Rejected";
                    public static readonly int Value = 4;
                }

                public static class QualificationDeleted
                {
                    public static readonly string Name = "Qualification-Deleted";
                    public static readonly int Value = 5;
                }

                public static class QualificationUpdated
                {
                    public static readonly string Name = "Qualification-Updated";
                    public static readonly int Value = 6;
                }

            }

            public static class Credential
            {
                public static readonly string Name = "Credential";
                public static readonly int Value = 9;
                public static class CredentialAdd
                {
                    public static readonly string Name = "Credential-Add";
                    public static readonly int Value = 1;
                }

                public static class CredentialExpired
                {
                    public static readonly string Name = "Credential-Expired";
                    public static readonly int Value = 2;
                }

                public static class CredentialApproved
                {
                    public static readonly string Name = "Credential-Approved";
                    public static readonly int Value = 3;
                }

                public static class CredentialRejected
                {
                    public static readonly string Name = "Credential-Rejected";
                    public static readonly int Value = 4;
                }

                public static class CredentialDeleted
                {
                    public static readonly string Name = "Credential-Deleted";
                    public static readonly int Value = 5;
                }

                public static class CredentialUpdated
                {
                    public static readonly string Name = "Credential-Updated";
                    public static readonly int Value = 6;
                }

                public static class CredentialExpireByApprove
                {
                    public static readonly string Name = "Credential-ExpireByApprove";
                    public static readonly int Value = 7;
                }

                public static class CredentialExpireByCancel
                {
                    public static readonly string Name = "Credential-ExpireByCancel";
                    public static readonly int Value = 8;
                }

                public static class CredentialExpireByExpire
                {
                    public static readonly string Name = "Credential-ExpireByExpire";
                    public static readonly int Value = 9;
                }

                public static class CredentialView
                {
                    public static readonly string Name = "Credential-View";
                    public static readonly int Value = 10;
                }


            }

            public static class License
            {
                public static readonly string Name = "License";
                public static readonly int Value = 10;
                public static class LicenseAdd
                {
                    public static readonly string Name = "License-Add";
                    public static readonly int Value = 1;
                }

                public static class LicenseExpired
                {
                    public static readonly string Name = "License-Expired";
                    public static readonly int Value = 2;
                }

                public static class LicenseDeleted
                {
                    public static readonly string Name = "License-Deleted";
                    public static readonly int Value = 3;
                }

                public static class LicenseUpdated
                {
                    public static readonly string Name = "License-Updated";
                    public static readonly int Value = 4;
                }

            }

            public static class Family
            {
                public static readonly string Name = "Family";
                public static readonly int Value = 11;
                public static class FamilyAdd
                {
                    public static readonly string Name = "Family-Add";
                    public static readonly int Value = 1;
                }

                public static class FamilyRemoved
                {
                    public static readonly string Name = "Family-Removed";
                    public static readonly int Value = 2;
                }

                public static class FamilyDeleted
                {
                    public static readonly string Name = "Family-Deleted";
                    public static readonly int Value = 3;
                }

            }

        }

        public static class Family
        {
            public static readonly string Name = "Family";
            public static readonly int Value = 4;
            public static class General
            {
                public static readonly string Name = "General";
                public static readonly int Value = 0;
            }
        }

        public static class Security
        {
            public static readonly string Name = "Security";
            public static readonly int Value = 5;
            public static class General
            {
                public static readonly string Name = "General";
                public static readonly int Value = 0;
            }

            public static class Role
            {
                public static readonly string Name = "Role";
                public static readonly int Value = 1;
                public static class RoleAdd
                {
                    public static readonly string Name = "Role-Add";
                    public static readonly int Value = 1;
                }

                public static class RoleCreate
                {
                    public static readonly string Name = "Role-Create";
                    public static readonly int Value = 2;
                }

                public static class RoleUpdate
                {
                    public static readonly string Name = "Role-Update";
                    public static readonly int Value = 3;
                }

                public static class RoleDelete
                {
                    public static readonly string Name = "Role-Delete";
                    public static readonly int Value = 4;
                }

            }

            public static class Group
            {
                public static readonly string Name = "Group";
                public static readonly int Value = 2;
                public static class GroupAdd
                {
                    public static readonly string Name = "Group-Add";
                    public static readonly int Value = 1;
                }

                public static class GroupCreate
                {
                    public static readonly string Name = "Group-Create";
                    public static readonly int Value = 2;
                }

                public static class GroupUpdate
                {
                    public static readonly string Name = "Group-Update";
                    public static readonly int Value = 3;
                }

                public static class GroupDelete
                {
                    public static readonly string Name = "Group-Delete";
                    public static readonly int Value = 4;
                }

            }

            public static class PasswordResetEmail
            {
                public static readonly string Name = "Password Reset Email";
                public static readonly int Value = 3;
            }

            public static class GeneratePassword
            {
                public static readonly string Name = "Generate Password";
                public static readonly int Value = 4;
            }

            public static class PasswordChanged
            {
                public static readonly string Name = "Password Changed";
                public static readonly int Value = 5;
            }

            public static class EnableDisableUser
            {
                public static readonly string Name = "Enable,Disable User";
                public static readonly int Value = 6;
            }

        }

        public static class Membership
        {
            public static readonly string Name = "Membership";
            public static readonly int Value = 6;
            public static class General
            {
                public static readonly string Name = "General";
                public static readonly int Value = 0;
            }

            public static class NewMembershipCreated
            {
                public static readonly string Name = "New Membership Created";
                public static readonly int Value = 1;
            }

            public static class MembershipChanged
            {
                public static readonly string Name = "Membership Changed";
                public static readonly int Value = 2;
                public static class BasicDetails
                {
                    public static readonly string Name = "Basic Details";
                    public static readonly int Value = 1;
                }

                public static class Price
                {
                    public static readonly string Name = "Price";
                    public static readonly int Value = 2;
                }

                public static class Subscription
                {
                    public static readonly string Name = "Subscription";
                    public static readonly int Value = 3;
                }

                public static class Installments
                {
                    public static readonly string Name = "Installments";
                    public static readonly int Value = 4;
                }

                public static class Tax
                {
                    public static readonly string Name = "Tax";
                    public static readonly int Value = 5;
                }

                public static class Benefits
                {
                    public static readonly string Name = "Benefits";
                    public static readonly int Value = 6;
                }

                public static class PurchaseRuleAdded
                {
                    public static readonly string Name = "Purchase Rule-Added";
                    public static readonly int Value = 7;
                }

                public static class PurchaseRuleUpdated
                {
                    public static readonly string Name = "Purchase Rule-Updated";
                    public static readonly int Value = 8;
                }

                public static class PurchaseRuleDeleted
                {
                    public static readonly string Name = "Purchase Rule-Deleted";
                    public static readonly int Value = 9;
                }

                public static class DiscountRuleAdded
                {
                    public static readonly string Name = "Discount Rule-Added";
                    public static readonly int Value = 10;
                }

                public static class DiscountRuleUpdated
                {
                    public static readonly string Name = "Discount Rule-Updated";
                    public static readonly int Value = 11;
                }

                public static class DiscountRuleDeleted
                {
                    public static readonly string Name = "Discount Rule-Deleted";
                    public static readonly int Value = 12;
                }

                public static class SurchargeRuleAdded
                {
                    public static readonly string Name = "Surcharge Rule-Added";
                    public static readonly int Value = 13;
                }

                public static class SurchargeRuleUpdated
                {
                    public static readonly string Name = "Surcharge Rule-Updated";
                    public static readonly int Value = 14;
                }

                public static class SurchargeRuleDeleted
                {
                    public static readonly string Name = "Surcharge Rule-Deleted";
                    public static readonly int Value = 15;
                }

                public static class AdditionalOptins
                {
                    public static readonly string Name = "Additional-Optins";
                    public static readonly int Value = 16;
                }

                public static class AdditionalCredential
                {
                    public static readonly string Name = "Additional-Credential";
                    public static readonly int Value = 17;
                }

                public static class AdditionalQualification
                {
                    public static readonly string Name = "Additional-Qualification";
                    public static readonly int Value = 18;
                }

                public static class AdditionalEmergencyContact
                {
                    public static readonly string Name = "Additional-Emergency Contact";
                    public static readonly int Value = 19;
                }

                public static class AdditionalUpsellProduct
                {
                    public static readonly string Name = "Additional-Upsell Product";
                    public static readonly int Value = 20;
                }

                public static class AdditionalFields
                {
                    public static readonly string Name = "Additional-Fields";
                    public static readonly int Value = 21;
                }

                public static class Section
                {
                    public static readonly string Name = "Section";
                    public static readonly int Value = 22;
                }

                public static class MembershipStartDateEndDateSettings
                {
                    public static readonly string Name = "MembershipStartDateEndDateSettings";
                    public static readonly int Value = 23;
                }

                public static class DataCaptureItemsAdded
                {
                    public static readonly string Name = "dataCaptureItems Added";
                    public static readonly int Value = 24;
                }

                public static class DataCaptureItemsUpdated
                {
                    public static readonly string Name = "dataCaptureItems Updated";
                    public static readonly int Value = 25;
                }

                public static class DataCaptureItemsDeleted
                {
                    public static readonly string Name = "dataCaptureItems Deleted";
                    public static readonly int Value = 26;
                }

            }

            public static class MembershipDeleteArchived
            {
                public static readonly string Name = "Membership Delete,Archived";
                public static readonly int Value = 3;
            }

            public static class MembershipSynched
            {
                public static readonly string Name = "Membership Synched";
                public static readonly int Value = 4;
                public static class BasicDetails
                {
                    public static readonly string Name = "Basic Details";
                    public static readonly int Value = 1;
                }

                public static class Price
                {
                    public static readonly string Name = "Price";
                    public static readonly int Value = 2;
                }

                public static class Subscription
                {
                    public static readonly string Name = "Subscription";
                    public static readonly int Value = 3;
                }

                public static class Installments
                {
                    public static readonly string Name = "Installments";
                    public static readonly int Value = 4;
                }

                public static class Tax
                {
                    public static readonly string Name = "Tax";
                    public static readonly int Value = 5;
                }

                public static class Benefits
                {
                    public static readonly string Name = "Benefits";
                    public static readonly int Value = 6;
                }

                public static class PurchaseRuleAdded
                {
                    public static readonly string Name = "Purchase Rule-Added";
                    public static readonly int Value = 7;
                }

                public static class PurchaseRuleUpdated
                {
                    public static readonly string Name = "Purchase Rule-Updated";
                    public static readonly int Value = 8;
                }

                public static class PurchaseRuleDeleted
                {
                    public static readonly string Name = "Purchase Rule-Deleted";
                    public static readonly int Value = 9;
                }

                public static class DiscountRuleAdded
                {
                    public static readonly string Name = "Discount Rule-Added";
                    public static readonly int Value = 10;
                }

                public static class DiscountRuleUpdated
                {
                    public static readonly string Name = "Discount Rule-Updated";
                    public static readonly int Value = 11;
                }

                public static class DiscountRuleDeleted
                {
                    public static readonly string Name = "Discount Rule-Deleted";
                    public static readonly int Value = 12;
                }

                public static class SurchargeRuleAdded
                {
                    public static readonly string Name = "Surcharge Rule-Added";
                    public static readonly int Value = 13;
                }

                public static class SurchargeRuleUpdated
                {
                    public static readonly string Name = "Surcharge Rule-Updated";
                    public static readonly int Value = 14;
                }

                public static class SurchargeRuleDeleted
                {
                    public static readonly string Name = "Surcharge Rule-Deleted";
                    public static readonly int Value = 15;
                }

                public static class AdditionalOptins
                {
                    public static readonly string Name = "Additional-Optins";
                    public static readonly int Value = 16;
                }

                public static class AdditionalCredential
                {
                    public static readonly string Name = "Additional-Credential";
                    public static readonly int Value = 17;
                }

                public static class AdditionalQualification
                {
                    public static readonly string Name = "Additional-Qualification";
                    public static readonly int Value = 18;
                }

                public static class AdditionalEmergencyContact
                {
                    public static readonly string Name = "Additional-Emergency Contact";
                    public static readonly int Value = 19;
                }

                public static class AdditionalUpsellProduct
                {
                    public static readonly string Name = "Additional-Upsell Product";
                    public static readonly int Value = 20;
                }

                public static class AdditionalFields
                {
                    public static readonly string Name = "Additional-Fields";
                    public static readonly int Value = 21;
                }

                public static class Section
                {
                    public static readonly string Name = "Section";
                    public static readonly int Value = 22;
                }

            }

        }

        public static class Eventcourse
        {
            public static readonly string Name = "Event,Course";
            public static readonly int Value = 7;
            public static class General
            {
                public static readonly string Name = "General";
                public static readonly int Value = 0;
            }

            public static class DetailsChanged
            {
                public static readonly string Name = "Details Changed";
                public static readonly int Value = 1;
            }

            public static class PriceChange
            {
                public static readonly string Name = "Price Change";
                public static readonly int Value = 2;
            }

            public static class AvailablePlaceChange
            {
                public static readonly string Name = "Available Place change";
                public static readonly int Value = 3;
            }

            public static class CourseBooking
            {
                public static readonly string Name = "Course Booking";
                public static readonly int Value = 4;
                public static class Created
                {
                    public static readonly string Name = "Created";
                    public static readonly int Value = 1;
                }

                public static class ChangedStatus
                {
                    public static readonly string Name = "Changed Status";
                    public static readonly int Value = 2;
                }

            }

            public static class EndDateChange
            {
                public static readonly string Name = "End Date Change";
                public static readonly int Value = 5;
            }

            public static class StartDateChange
            {
                public static readonly string Name = "Start Date Change";
                public static readonly int Value = 6;
            }

            public static class EventContacts
            {
                public static readonly string Name = "Event Contacts";
                public static readonly int Value = 7;
                public static class EventContactsAdd
                {
                    public static readonly string Name = "Event Contacts-Add";
                    public static readonly int Value = 1;
                }

                public static class EventContactsRemove
                {
                    public static readonly string Name = "Event Contacts-Remove";
                    public static readonly int Value = 2;
                }

                public static class EventContactsUpdate
                {
                    public static readonly string Name = "Event Contacts-Update";
                    public static readonly int Value = 3;
                }

            }

            public static class EventSettings
            {
                public static readonly string Name = "Event Settings";
                public static readonly int Value = 8;
                public static class MadePublic
                {
                    public static readonly string Name = "Made Public";
                    public static readonly int Value = 1;
                }

                public static class MadePrivate
                {
                    public static readonly string Name = "Made Private";
                    public static readonly int Value = 2;
                }

                public static class MadeFeatureEvent
                {
                    public static readonly string Name = "Made Feature Event";
                    public static readonly int Value = 3;
                }

            }

            public static class Achivement
            {
                public static readonly string Name = "Achivement";
                public static readonly int Value = 9;
                public static class CredentialAdd
                {
                    public static readonly string Name = "Credential-Add";
                    public static readonly int Value = 1;
                }

                public static class CredentialDeleted
                {
                    public static readonly string Name = "Credential-Deleted";
                    public static readonly int Value = 2;
                }

                public static class QualificationAdd
                {
                    public static readonly string Name = "Qualification-Add";
                    public static readonly int Value = 3;
                }

                public static class QualificationDeleted
                {
                    public static readonly string Name = "Qualification-Deleted";
                    public static readonly int Value = 4;
                }

            }

            public static class Attachment
            {
                public static readonly string Name = "Attachment";
                public static readonly int Value = 10;
                public static class AttachmentAdd
                {
                    public static readonly string Name = "Attachment-Add";
                    public static readonly int Value = 1;
                }

                public static class AttachmentRemove
                {
                    public static readonly string Name = "Attachment-Remove";
                    public static readonly int Value = 2;
                }

            }

            public static class Invitee
            {
                public static readonly string Name = "Invitee";
                public static readonly int Value = 12;
                public static class InviteeAdd
                {
                    public static readonly string Name = "Invitee-Add";
                    public static readonly int Value = 1;
                }

                public static class InviteeRemove
                {
                    public static readonly string Name = "Invitee-Remove";
                    public static readonly int Value = 2;
                }

            }

            public static class Ticket
            {
                public static readonly string Name = "Ticket";
                public static readonly int Value = 13;
                public static class TicketAdd
                {
                    public static readonly string Name = "Ticket-Add";
                    public static readonly int Value = 1;
                }

                public static class TicketRemove
                {
                    public static readonly string Name = "Ticket-Remove";
                    public static readonly int Value = 2;
                }

                public static class TicketUpdate
                {
                    public static readonly string Name = "Ticket-Update";
                    public static readonly int Value = 3;
                }

                public static class TicketAvailablePlaceChange
                {
                    public static readonly string Name = "Ticket-Available Place change";
                    public static readonly int Value = 4;
                }

                public static class TicketBookingEndDateChange
                {
                    public static readonly string Name = "Ticket-Booking End Date Change";
                    public static readonly int Value = 5;
                }

                public static class TicketPrice
                {
                    public static readonly string Name = "Ticket-Price";
                    public static readonly int Value = 6;
                }

                public static class TicketTax
                {
                    public static readonly string Name = "Ticket-Tax";
                    public static readonly int Value = 7;
                }

                public static class TicketPurchaseRuleAdded
                {
                    public static readonly string Name = "Ticket-Purchase Rule-Added";
                    public static readonly int Value = 8;
                }

                public static class TicketPurchaseRuleUpdated
                {
                    public static readonly string Name = "Ticket-Purchase Rule-Updated";
                    public static readonly int Value = 9;
                }

                public static class TicketPurchaseRuleDeleted
                {
                    public static readonly string Name = "Ticket-Purchase Rule-Deleted";
                    public static readonly int Value = 10;
                }

                public static class TicketDiscountRuleAdded
                {
                    public static readonly string Name = "Ticket-Discount Rule-Added";
                    public static readonly int Value = 11;
                }

                public static class TicketDiscountRuleUpdated
                {
                    public static readonly string Name = "Ticket-Discount Rule-Updated";
                    public static readonly int Value = 12;
                }

                public static class TicketDiscountRuleDeleted
                {
                    public static readonly string Name = "Ticket-Discount Rule-Deleted";
                    public static readonly int Value = 13;
                }

                public static class TicketSurchargeRuleAdded
                {
                    public static readonly string Name = "Ticket-Surchange Rule-Added";
                    public static readonly int Value = 14;
                }

                public static class TicketSurchargeRuleUpdated
                {
                    public static readonly string Name = "Ticket-Surchange Rule-Updated";
                    public static readonly int Value = 15;
                }

                public static class TicketSurchargeRuleDeleted
                {
                    public static readonly string Name = "Ticket-Surchange Rule-Deleted";
                    public static readonly int Value = 16;
                }

                public static class TicketAdditionalOptins
                {
                    public static readonly string Name = "Ticket-Additional-Optins";
                    public static readonly int Value = 17;
                }

                public static class TicketAdditionalCredential
                {
                    public static readonly string Name = "Ticket-Additional-Credential";
                    public static readonly int Value = 18;
                }

                public static class TicketAdditionalQualification
                {
                    public static readonly string Name = "Ticket-Additional-Qualification";
                    public static readonly int Value = 19;
                }

                public static class TicketAdditionalEmergencyContact
                {
                    public static readonly string Name = "Ticket-Additional-Emergency Contact";
                    public static readonly int Value = 20;
                }

                public static class TicketAdditionalUpsellProduct
                {
                    public static readonly string Name = "Ticket-Additional-Upsell Product";
                    public static readonly int Value = 21;
                }

                public static class TicketAdditionalFields
                {
                    public static readonly string Name = "Ticket-Additional-Fields";
                    public static readonly int Value = 22;
                }

            }

        }

        public static class Club
        {
            public static readonly string Name = "Club";
            public static readonly int Value = 8;
            public static class General
            {
                public static readonly string Name = "General";
                public static readonly int Value = 0;
            }

            public static class DetailsChanged
            {
                public static readonly string Name = "Details Changed";
                public static readonly int Value = 1;
                public static class LocationChanged
                {
                    public static readonly string Name = "Location Changed";
                    public static readonly int Value = 1;
                }

                public static class LatLongChanged
                {
                    public static readonly string Name = "Lat-long Changed";
                    public static readonly int Value = 2;
                }

            }

            public static class PriceChange
            {
                public static readonly string Name = "Price Change";
                public static readonly int Value = 2;
            }

            public static class AvailablePlaceChange
            {
                public static readonly string Name = "Available Place change";
                public static readonly int Value = 3;
            }

            public static class CourseBooking
            {
                public static readonly string Name = "Course Booking";
                public static readonly int Value = 4;
            }
            public static class Affiliation
            {
                public static readonly string Name = "Affiliation";
                public static readonly int Value = 5;
                public static class AffiliationAdded
                {
                    public static readonly string Name = "Affiliation-Added";
                    public static readonly int Value = 1;
                }

                public static class AffiliationUpdated
                {
                    public static readonly string Name = "Affiliation-Updated";
                    public static readonly int Value = 2;
                }

                public static class AffiliationDeleted
                {
                    public static readonly string Name = "Affiliation-Deleted";
                    public static readonly int Value = 3;
                }

            }

        }

        public static class Reports
        {
            public static readonly string Name = "Reports";
            public static readonly int Value = 9;
            public static class General
            {
                public static readonly string Name = "General";
                public static readonly int Value = 0;
            }

            public static class Executed
            {
                public static readonly string Name = "Executed";
                public static readonly int Value = 1;
            }

        }

        public static class ClubPlus
        {
            public static readonly string Name = "Club Plus";
            public static readonly int Value = 10;
            public static class General
            {
                public static readonly string Name = "General";
                public static readonly int Value = 0;
            }

            public static class CentralSystemAccessed
            {
                public static readonly string Name = "Central System Accessed";
                public static readonly int Value = 1;
            }

            public static class BackFromCentralSystem
            {
                public static readonly string Name = "Back From Central System";
                public static readonly int Value = 2;
            }

        }

        public static class Team
        {
            public static readonly string Name = "Team";
            public static readonly int Value = 11;
            public static class General
            {
                public static readonly string Name = "General";
                public static readonly int Value = 0;
            }

            public static class Join
            {
                public static readonly string Name = "Join";
                public static readonly int Value = 1;
            }

            public static class Leave
            {
                public static readonly string Name = "Leave";
                public static readonly int Value = 2;
            }

        }

        public static class Chat
        {
            public static readonly string Name = "Chat";
            public static readonly int Value = 12;
            public static class General
            {
                public static readonly string Name = "General";
                public static readonly int Value = 0;
            }

            public static class StateChange
            {
                public static readonly string Name = "State Change";
                public static readonly int Value = 1;
            }

            public static class Copy
            {
                public static readonly string Name = "Copy";
                public static readonly int Value = 2;
            }

        }

        public static class EmailManagement
        {
            public static readonly string Name = "Email Management";
            public static readonly int Value = 13;
            public static class General
            {
                public static readonly string Name = "General";
                public static readonly int Value = 0;
                public static class Created
                {
                    public static readonly string Name = "Created";
                    public static readonly int Value = 1;
                }

                public static class Deleted
                {
                    public static readonly string Name = "Deleted";
                    public static readonly int Value = 2;
                }

                public static class Updated
                {
                    public static readonly string Name = "Updated";
                    public static readonly int Value = 3;
                }

                public static class Active
                {
                    public static readonly string Name = "Active";
                    public static readonly int Value = 4;
                }

                public static class InActive
                {
                    public static readonly string Name = "InActive";
                    public static readonly int Value = 5;
                }

            }

            public static class Attachment
            {
                public static readonly string Name = "Attachment";
                public static readonly int Value = 1;
                public static class Added
                {
                    public static readonly string Name = "Added";
                    public static readonly int Value = 1;
                }

                public static class Deleted
                {
                    public static readonly string Name = "Deleted";
                    public static readonly int Value = 2;
                }

            }

            public static class Report
            {
                public static readonly string Name = "Report";
                public static readonly int Value = 2;
                public static class Added
                {
                    public static readonly string Name = "Added";
                    public static readonly int Value = 1;
                }

                public static class Deleted
                {
                    public static readonly string Name = "Deleted";
                    public static readonly int Value = 2;
                }

            }

            public static class EmailTemplate
            {
                public static readonly string Name = "Email Template";
                public static readonly int Value = 3;
                public static class Created
                {
                    public static readonly string Name = "Created";
                    public static readonly int Value = 1;
                }

                public static class Deleted
                {
                    public static readonly string Name = "Deleted";
                    public static readonly int Value = 2;
                }

                public static class Updated
                {
                    public static readonly string Name = "Updated";
                    public static readonly int Value = 3;
                }

                public static class BulkTemplateUpdated
                {
                    public static readonly string Name = "Bulk Template Updated";
                    public static readonly int Value = 4;
                }

            }

            public static class Scheme
            {
                public static readonly string Name = "Scheme";
                public static readonly int Value = 4;
                public static class Created
                {
                    public static readonly string Name = "Created";
                    public static readonly int Value = 1;
                }

                public static class Deleted
                {
                    public static readonly string Name = "Deleted";
                    public static readonly int Value = 2;
                }

                public static class Updated
                {
                    public static readonly string Name = "Updated";
                    public static readonly int Value = 3;
                }

                public static class Active
                {
                    public static readonly string Name = "Active";
                    public static readonly int Value = 4;
                }

                public static class InActive
                {
                    public static readonly string Name = "InActive";
                    public static readonly int Value = 5;
                }

            }

        }

        public static class FieldManagement
        {
            public static readonly string Name = "Field Management";
            public static readonly int Value = 14;
            public static class General
            {
                public static readonly string Name = "General";
                public static readonly int Value = 0;
            }

            public static class Profile
            {
                public static readonly string Name = "Profile";
                public static readonly int Value = 1;
                public static class ProfileFieldAdd
                {
                    public static readonly string Name = "Profile Field-Add";
                    public static readonly int Value = 1;
                }

                public static class ProfileFieldUpdate
                {
                    public static readonly string Name = "Profile Field-Update";
                    public static readonly int Value = 2;
                }

                public static class ProfileFieldDelete
                {
                    public static readonly string Name = "Profile Field-Delete";
                    public static readonly int Value = 3;
                }

            }

            public static class Event
            {
                public static readonly string Name = "Event";
                public static readonly int Value = 2;
                public static class EventFieldAdd
                {
                    public static readonly string Name = "Event Field-Add";
                    public static readonly int Value = 1;
                }

                public static class EventFieldUpdate
                {
                    public static readonly string Name = "Event Field-Update";
                    public static readonly int Value = 2;
                }

                public static class EventFieldDelete
                {
                    public static readonly string Name = "Event Field-Delete";
                    public static readonly int Value = 3;
                }

            }

            public static class Qualification
            {
                public static readonly string Name = "Qualification";
                public static readonly int Value = 3;
                public static class QualificationFieldAdd
                {
                    public static readonly string Name = "Qualification Field-Add";
                    public static readonly int Value = 1;
                }

                public static class QualificationFieldUpdate
                {
                    public static readonly string Name = "Qualification Field-Update";
                    public static readonly int Value = 2;
                }

                public static class QualificationFieldDelete
                {
                    public static readonly string Name = "Qualification Field-Delete";
                    public static readonly int Value = 3;
                }

            }

            public static class Credential
            {
                public static readonly string Name = "Credential";
                public static readonly int Value = 4;
                public static class CredentialFieldAdd
                {
                    public static readonly string Name = "Credential Field-Add";
                    public static readonly int Value = 1;
                }

                public static class CredentialFieldUpdate
                {
                    public static readonly string Name = "Credential Field-Update";
                    public static readonly int Value = 2;
                }

                public static class CredentialFieldDelete
                {
                    public static readonly string Name = "Credential Field-Delete";
                    public static readonly int Value = 3;
                }

            }

            public static class SynchedFromMirror
            {
                public static readonly string Name = "Synched From Mirror";
                public static readonly int Value = 5;
            }

        }

        public static class LegueManagement
        {
            public static readonly string Name = "Legue Management";
            public static readonly int Value = 15;
            public static class General
            {
                public static readonly string Name = "General";
                public static readonly int Value = 0;
            }
        }

        public static class Payment
        {
            public static readonly string Name = "Payment";
            public static readonly int Value = 16;
            public static class General
            {
                public static readonly string Name = "General";
                public static readonly int Value = 0;
            }

            public static class Purchase
            {
                public static readonly string Name = "Purchase";
                public static readonly int Value = 1;
            }

            public static class Invoice
            {
                public static readonly string Name = "Invoice";
                public static readonly int Value = 2;
                public static class InvoiceCreated
                {
                    public static readonly string Name = "Invoice-Created";
                    public static readonly int Value = 1;
                }

                public static class InvoiceUpdated
                {
                    public static readonly string Name = "Invoice-Updated";
                    public static readonly int Value = 2;
                }

                public static class InvoiceDeleted
                {
                    public static readonly string Name = "Invoice-Deleted";
                    public static readonly int Value = 3;
                }

                public static class InvoiceFailed
                {
                    public static readonly string Name = "Invoice-Failed";
                    public static readonly int Value = 4;
                }

                public static class InvoiceCancelled
                {
                    public static readonly string Name = "Invoice-Cancled"; // Note: spelling preserved from your input
                    public static readonly int Value = 5;
                }

            }

            public static class Refund
            {
                public static readonly string Name = "Refund";
                public static readonly int Value = 3;
            }

            public static class FailedPayment
            {
                public static readonly string Name = "Failed Payment";
                public static readonly int Value = 4;
            }

            public static class SynchedFromMirror
            {
                public static readonly string Name = "Synched From Mirror";
                public static readonly int Value = 5;
            }

            public static class ReceiptStatusChanged
            {
                public static readonly string Name = "Receipt Status Changed";
                public static readonly int Value = 6;
            }

            public static class PaymentUserChanged
            {
                public static readonly string Name = "Payment User Changed";
                public static readonly int Value = 7;
            }

            public static class Subscription
            {
                public static readonly string Name = "Subscription";
                public static readonly int Value = 8;
                public static class SubscriptionCreated
                {
                    public static readonly string Name = "Subscription-Created";
                    public static readonly int Value = 1;
                }

                public static class SubscriptionUpdated
                {
                    public static readonly string Name = "Subscription-Updated";
                    public static readonly int Value = 2;
                }

                public static class SubscriptionFailed
                {
                    public static readonly string Name = "Subscription-Failed";
                    public static readonly int Value = 3;
                }

            }

            public static class Installment
            {
                public static readonly string Name = "Installment";
                public static readonly int Value = 9;
                public static class InstallmentCreated
                {
                    public static readonly string Name = "Installment-Created";
                    public static readonly int Value = 1;
                }

                public static class InstallmentUpdated
                {
                    public static readonly string Name = "Installment-Updated";
                    public static readonly int Value = 2;
                }

                public static class InstallmentFailed
                {
                    public static readonly string Name = "Installment-Failed";
                    public static readonly int Value = 3;
                }

            }

            public static class MandateSetup
            {
                public static readonly string Name = "Mandate Setup";
                public static readonly int Value = 10;
            }

            public static class MarchentProfile
            {
                public static readonly string Name = "Marchent Profile";
                public static readonly int Value = 11;
                public static class MarchentProfileCreated
                {
                    public static readonly string Name = "Marchent Profile-Created";
                    public static readonly int Value = 1;
                }

                public static class MarchentProfileUpdated
                {
                    public static readonly string Name = "Marchent Profile-Updated";
                    public static readonly int Value = 2;
                }

                public static class MarchentProfileDeleted
                {
                    public static readonly string Name = "Marchent Profile-Deleted";
                    public static readonly int Value = 3;
                }

            }

        }

        public static class BulkImport
        {
            public static readonly string Name = "Bulk Import";
            public static readonly int Value = 17;
            public static class General
            {
                public static readonly string Name = "General";
                public static readonly int Value = 0;
            }
        }

        public static class SystemUpgradechangedsupport
        {
            public static readonly string Name = "System Upgrade,Changed,Support";
            public static readonly int Value = 18;
            public static class General
            {
                public static readonly string Name = "General";
                public static readonly int Value = 0;
            }

            public static class FilePatched
            {
                public static readonly string Name = "File Patched";
                public static readonly int Value = 1;
            }

            public static class Rule
            {
                public static readonly string Name = "Rule";
                public static readonly int Value = 2;
            }

            public static class Refund
            {
                public static readonly string Name = "Refund";
                public static readonly int Value = 3;
            }

            public static class Setting
            {
                public static readonly string Name = "Setting";
                public static readonly int Value = 4;
            }

            public static class ExchangeRateChanged
            {
                public static readonly string Name = "Exchange Rate Changed";
                public static readonly int Value = 5;
            }

            public static class PaymentConfigChanged
            {
                public static readonly string Name = "Payment Config Changed";
                public static readonly int Value = 6;
            }

            public static class DbChangedManually
            {
                public static readonly string Name = "DB Changed Manually";
                public static readonly int Value = 7;
            }

            public static class DbBulkUpdate
            {
                public static readonly string Name = "Db Bulk Update";
                public static readonly int Value = 7; // ⚠️ Duplicate value as above (consider fixing if needed)
            }

            public static class SupportLogin
            {
                public static readonly string Name = "Support Login";
                public static readonly int Value = 8;
            }

            public static class SupportEmailTriggered
            {
                public static readonly string Name = "Support Email Triggered";
                public static readonly int Value = 9;
            }

            public static class WorkbenchImport
            {
                public static readonly string Name = "Workbench Import";
                public static readonly int Value = 10;
            }

            public static class RepositoryImport
            {
                public static readonly string Name = "Repository Import";
                public static readonly int Value = 11;
            }

            public static class Scripting
            {
                public static readonly string Name = "Scripting";
                public static readonly int Value = 12;
            }

            public static class Form
            {
                public static readonly string Name = "Form";
                public static readonly int Value = 13;
            }

            public static class Workbench
            {
                public static readonly string Name = "Workbench";
                public static readonly int Value = 14;
            }

        }

        public static class BackgroundProcess
        {
            public static readonly string Name = "Background Process";
            public static readonly int Value = 19;
            public static class General
            {
                public static readonly string Name = "General";
                public static readonly int Value = 0;
            }

            public static class RecurrentPayment
            {
                public static readonly string Name = "Recurrent payment";
                public static readonly int Value = 1;
            }

            public static class EmailSend
            {
                public static readonly string Name = "Email Send";
                public static readonly int Value = 2;
            }

            public static class ScehduleJob
            {
                public static readonly string Name = "Scehdule Job";
                public static readonly int Value = 3;
            }

            public static class IntegrationExe
            {
                public static readonly string Name = "Integration Exe";
                public static readonly int Value = 4;
            }

            public static class ChatViewMailDump
            {
                public static readonly string Name = "Chat-View Mail Dump";
                public static readonly int Value = 5;
            }

        }

        public static class Login
        {
            public static readonly string Name = "Login";
            public static readonly int Value = 20;
            public static class General
            {
                public static readonly string Name = "General";
                public static readonly int Value = 0;
            }

            public static class FailedLoginAttempt
            {
                public static readonly string Name = "Failed Login Attempt";
                public static readonly int Value = 1;
            }

            public static class SuccessfulLogin
            {
                public static readonly string Name = "Successful Login";
                public static readonly int Value = 2;
            }

            public static class GenerateLoginToken
            {
                public static readonly string Name = "Generate Login Token";
                public static readonly int Value = 3;
            }

        }

        public static class ShoppingCart
        {
            public static readonly string Name = "Shopping Cart";
            public static readonly int Value = 21;
            public static class General
            {
                public static readonly string Name = "General";
                public static readonly int Value = 0;
            }

            public static class PurchaseRuleFailed
            {
                public static readonly string Name = "Purchase Rule Failed";
                public static readonly int Value = 1;
            }

            public static class QuantityChanged
            {
                public static readonly string Name = "Quantity Changed";
                public static readonly int Value = 2;
            }

            public static class PaymentFailed
            {
                public static readonly string Name = "Payment Failed";
                public static readonly int Value = 3;
            }

        }

        public static class ClubMember
        {
            public static readonly string Name = "Club Member";
            public static readonly int Value = 22;
            public static class General
            {
                public static readonly string Name = "General";
                public static readonly int Value = 0;
                public static class Created
                {
                    public static readonly string Name = "Created";
                    public static readonly int Value = 1;
                }

                public static class Updated
                {
                    public static readonly string Name = "Updated";
                    public static readonly int Value = 2;
                }

                public static class Join
                {
                    public static readonly string Name = "Join";
                    public static readonly int Value = 3;
                }

                public static class Leave
                {
                    public static readonly string Name = "Leave";
                    public static readonly int Value = 4;
                }

                public static class RolesUpdate
                {
                    public static readonly string Name = "Roles Update";
                    public static readonly int Value = 5;
                }

                public static class PrimaryClubUpdate
                {
                    public static readonly string Name = "Primary Club Update";
                    public static readonly int Value = 6;
                }

                public static class IsHiddenUpdate
                {
                    public static readonly string Name = "IsHidden Update";
                    public static readonly int Value = 7;
                }

            }

            public static class Join
            {
                public static readonly string Name = "Join";
                public static readonly int Value = 1;
            }

            public static class Leave
            {
                public static readonly string Name = "Leave";
                public static readonly int Value = 2;
            }

        }

        public static class Finance
        {
            public static readonly string Name = "Finance";
            public static readonly int Value = 23;
            public static class General
            {
                public static readonly string Name = "General";
                public static readonly int Value = 0;
                public static class General1
                {
                    public static readonly string Name = "General";
                    public static readonly int Value = 1;
                }

            }

            public static class PaymentSetup
            {
                public static readonly string Name = "Payment Setup";
                public static readonly int Value = 1;
                public static class AccountCreated
                {
                    public static readonly string Name = "Account Created";
                    public static readonly int Value = 1;
                }

                public static class AccountUpdated
                {
                    public static readonly string Name = "Account Updated";
                    public static readonly int Value = 2;
                }

                public static class ViewDashboard
                {
                    public static readonly string Name = "View Dashboard";
                    public static readonly int Value = 3;
                }

                public static class PayoutScheduleUpdated
                {
                    public static readonly string Name = "Payout Schedule Updated";
                    public static readonly int Value = 4;
                }

                public static class Export
                {
                    public static readonly string Name = "Export";
                    public static readonly int Value = 5;
                }

                public static class AccountDeleted
                {
                    public static readonly string Name = "Account Deleted";
                    public static readonly int Value = 6;
                }

            }

            public static class AccountSetup
            {
                public static readonly string Name = "Account Setup";
                public static readonly int Value = 2;
                public static class BankDetailsUpdated
                {
                    public static readonly string Name = "BankDetails Updated";
                    public static readonly int Value = 1;
                }

                public static class CardDetailsUpdated
                {
                    public static readonly string Name = "CardDetails Updated";
                    public static readonly int Value = 2;
                }

                public static class BillingDetailsUpdated
                {
                    public static readonly string Name = "BillingDetails Updated";
                    public static readonly int Value = 3;
                }

                public static class CancellationRequest
                {
                    public static readonly string Name = "Cancellation Request";
                    public static readonly int Value = 4;
                }

                public static class Upgrade
                {
                    public static readonly string Name = "Upgrade";
                    public static readonly int Value = 5;
                }

                public static class RevertCancellationRequest
                {
                    public static readonly string Name = "Revert Cancellation Request";
                    public static readonly int Value = 6;
                }

            }

        }

        public static class Communication
        {
            public static readonly string Name = "Communication";
            public static readonly int Value = 24;
            public static class General
            {
                public static readonly string Name = "General";
                public static readonly int Value = 0;
                public static class General1
                {
                    public static readonly string Name = "General";
                    public static readonly int Value = 1;
                }

                public static class ExportStart
                {
                    public static readonly string Name = "ExportStart";
                    public static readonly int Value = 2;
                }

                public static class ExportFinish
                {
                    public static readonly string Name = "ExportFinish";
                    public static readonly int Value = 3;
                }

            }

            public static class Email
            {
                public static readonly string Name = "Email";
                public static readonly int Value = 1;
                public static class Created
                {
                    public static readonly string Name = "Created";
                    public static readonly int Value = 1;
                }

                public static class Updated
                {
                    public static readonly string Name = "Updated";
                    public static readonly int Value = 2;
                }

                public static class Deleted
                {
                    public static readonly string Name = "Deleted";
                    public static readonly int Value = 3;
                }

                public static class ExportReport
                {
                    public static readonly string Name = "Export Report";
                    public static readonly int Value = 4;
                }

            }

            public static class Segment
            {
                public static readonly string Name = "Segment";
                public static readonly int Value = 2;
                public static class Created
                {
                    public static readonly string Name = "Created";
                    public static readonly int Value = 1;
                }

                public static class Updated
                {
                    public static readonly string Name = "Updated";
                    public static readonly int Value = 2;
                }

                public static class Deleted
                {
                    public static readonly string Name = "Deleted";
                    public static readonly int Value = 3;
                }

                public static class ChangedStatus
                {
                    public static readonly string Name = "Changed Status";
                    public static readonly int Value = 4;
                }

                public static class ExportSegment
                {
                    public static readonly string Name = "Export Segment";
                    public static readonly int Value = 5;
                }

            }

        }

        public static class Mfa
        {
            public static readonly string Name = "MFA";
            public static readonly int Value = 25;
            public static class General
            {
                public static readonly string Name = "General";
                public static readonly int Value = 0;
                public static class Created
                {
                    public static readonly string Name = "Created";
                    public static readonly int Value = 1;
                }

                public static class Updated
                {
                    public static readonly string Name = "Updated";
                    public static readonly int Value = 2;
                }

                public static class Deleted
                {
                    public static readonly string Name = "Deleted";
                    public static readonly int Value = 3;
                }

                public static class Login
                {
                    public static readonly string Name = "Login";
                    public static readonly int Value = 4;
                }

            }

            public static class AuthenticatorApp
            {
                public static readonly string Name = "Authenticator App";
                public static readonly int Value = 1;
                public static class Created
                {
                    public static readonly string Name = "Created";
                    public static readonly int Value = 1;
                }

                public static class Deleted
                {
                    public static readonly string Name = "Deleted";
                    public static readonly int Value = 2;
                }

                public static class Login
                {
                    public static readonly string Name = "Login";
                    public static readonly int Value = 3;
                }

            }

            public static class WhatsApp
            {
                public static readonly string Name = "WhatsApp";
                public static readonly int Value = 2;
                public static class Created
                {
                    public static readonly string Name = "Created";
                    public static readonly int Value = 1;
                }

                public static class Updated
                {
                    public static readonly string Name = "Updated";
                    public static readonly int Value = 2;
                }

                public static class Deleted
                {
                    public static readonly string Name = "Deleted";
                    public static readonly int Value = 3;
                }

                public static class Login
                {
                    public static readonly string Name = "Login";
                    public static readonly int Value = 4;
                }

            }

        }

        public static class UserMerged
        {
            public static readonly string Name = "User Merged";
            public static readonly int Value = 26;
            public static class General
            {
                public static readonly string Name = "General";
                public static readonly int Value = 0;
                public static class Created
                {
                    public static readonly string Name = "Created";
                    public static readonly int Value = 1;
                }

                public static class Deleted
                {
                    public static readonly string Name = "Deleted";
                    public static readonly int Value = 2;
                }

            }
        }

        public static class Credential
        {
            public static readonly string Name = "Credential";
            public static readonly int Value = 27;
            public static class General
            {
                public static readonly string Name = "General";
                public static readonly int Value = 0;
            }

            public static class CredentialCreated
            {
                public static readonly string Name = "Credential Created";
                public static readonly int Value = 1;
            }

            public static class CredentialUpdated
            {
                public static readonly string Name = "Credential Updated";
                public static readonly int Value = 2;
            }

        }

        public static class ClassBooking
        {
            public static readonly string Name = "Class Booking";
            public static readonly int Value = 28;
            public static class General
            {
                public static readonly string Name = "General";
                public static readonly int Value = 0;
            }

            public static class DetailsChanged
            {
                public static readonly string Name = "Details Changed";
                public static readonly int Value = 1;
            }

            public static class PriceChange
            {
                public static readonly string Name = "Price Change";
                public static readonly int Value = 2;
            }

            public static class AvailablePlaceChange
            {
                public static readonly string Name = "Available Place change";
                public static readonly int Value = 3;
            }

            public static class EndDateChange
            {
                public static readonly string Name = "End Date Change";
                public static readonly int Value = 5;
            }

            public static class StartDateChange
            {
                public static readonly string Name = "Start Date Change";
                public static readonly int Value = 6;
            }

            public static class MembershipChanges
            {
                public static readonly string Name = "MembershipChanges";
                public static readonly int Value = 7;
            }

            public static class BookingContact
            {
                public static readonly string Name = "BookingContact";
                public static readonly int Value = 8;
            }

            public static class TrialChanges
            {
                public static readonly string Name = "Trial Changes";
                public static readonly int Value = 9;
            }

            public static class Waitlist
            {
                public static readonly string Name = "Waitlist";
                public static readonly int Value = 10;
            }

            public static class CancelBooking
            {
                public static readonly string Name = "Cancel Booking";
                public static readonly int Value = 11;
            }

        }

        public static class AssetManagement
        {
            public static readonly string Name = "Asset Management";
            public static readonly int Value = 29;

            public static class General
            {
                public static readonly string Name = "General";
                public static readonly int Value = 0;
                public static class Created
                {
                    public static readonly string Name = "Created";
                    public static readonly int Value = 1;
                }

                public static class Updated
                {
                    public static readonly string Name = "Updated";
                    public static readonly int Value = 2;
                }

                public static class Deleted
                {
                    public static readonly string Name = "Deleted";
                    public static readonly int Value = 3;
                }

                public static class Approved
                {
                    public static readonly string Name = "Approved";
                    public static readonly int Value = 4;
                }
                public static class Rejected
                {
                    public static readonly string Name = "Rejected";
                    public static readonly int Value = 5;
                }
            }

            public static class DetailsChanged
            {
                public static readonly string Name = "Details Changed";
                public static readonly int Value = 1;
            }

            public static class AdditionalDetailsChanged
            {
                public static readonly string Name = "Additional Details Changed";
                public static readonly int Value = 2;
            }

            public static class StatusChanged
            {
                public static readonly string Name = "Status Changed";
                public static readonly int Value = 3;
            }
            public static class OwnershipChanged
            {
                public static readonly string Name = "Ownership Changed";
                public static readonly int Value = 4;
            }

            // 1. Asset Attachment
            public static class AssetAttachment
            {
                public static readonly string Name = "Asset Attachment";
                public static readonly int Value = 5;

                public static class Added
                {
                    public static readonly string Name = "Added";
                    public static readonly int Value = 1;
                }
                public static class Removed
                {
                    public static readonly string Name = "Removed";
                    public static readonly int Value = 2;
                }
            }

            // 2. Asset Credential
            public static class AssetCredential
            {
                public static readonly string Name = "Asset Credential";
                public static readonly int Value = 6;

                public static class Created
                {
                    public static readonly string Name = "Created";
                    public static readonly int Value = 1;
                }
                public static class Updated
                {
                    public static readonly string Name = "Updated";
                    public static readonly int Value = 2;
                }
                public static class Deleted
                {
                    public static readonly string Name = "Deleted";
                    public static readonly int Value = 3;
                }
                public static class Approved
                {
                    public static readonly string Name = "Approved";
                    public static readonly int Value = 4;
                }
                public static class Rejected
                {
                    public static readonly string Name = "Rejected";
                    public static readonly int Value = 5;
                }
                public static class StatusChanged
                {
                    public static readonly string Name = "Status Changed";
                    public static readonly int Value = 6;
                }
            }

            public static class AssetLicense
            {
                public static readonly string Name = "Asset License";
                public static readonly int Value = 8;

                public static class Created
                {
                    public static readonly string Name = "Created";
                    public static readonly int Value = 1;
                }
                public static class Updated
                {
                    public static readonly string Name = "Updated";
                    public static readonly int Value = 2;
                }
                public static class Deleted
                {
                    public static readonly string Name = "Deleted";
                    public static readonly int Value = 3;
                }
                public static class Approved
                {
                    public static readonly string Name = "Approved";
                    public static readonly int Value = 4;
                }
                public static class Rejected
                {
                    public static readonly string Name = "Rejected";
                    public static readonly int Value = 5;
                }
                public static class StatusChanged
                {
                    public static readonly string Name = "Status Changed";
                    public static readonly int Value = 6;
                }

                public static class Canceled
                {
                    public static readonly string Name = "Canceled";
                    public static readonly int Value = 7;
                }
            }
            // 3. Asset Lease
            public static class AssetLease
            {
                public static readonly string Name = "Asset Lease";
                public static readonly int Value = 7;

                public static class Created
                {
                    public static readonly string Name = "Created";
                    public static readonly int Value = 1;
                }
                public static class Updated
                {
                    public static readonly string Name = "Updated";
                    public static readonly int Value = 2;
                }

                public static class Rejected
                {
                    public static readonly string Name = "Rejected";
                    public static readonly int Value = 4;
                }
                public static class Cancelled
                {
                    public static readonly string Name = "Cancelled";
                    public static readonly int Value = 5;
                }
                public static class Approved
                {
                    public static readonly string Name = "Approved";
                    public static readonly int Value = 6;
                }
                public static class Payment
                {
                    public static readonly string Name = "Payment";
                    public static readonly int Value = 7;
                }
                public static class ChangeStatus
                {
                    public static readonly string Name = "Change Status";
                    public static readonly int Value = 8;
                }
                public static class OwnerApproved
                {
                    public static readonly string Name = "Owner Approved";
                    public static readonly int Value = 9;
                }
            }

            // 4. Asset Transfer
            public static class AssetTransfer
            {
                public static readonly string Name = "Asset Transfer";
                public static readonly int Value = 4;

                public static class Created
                {
                    public static readonly string Name = "Created";
                    public static readonly int Value = 1;
                }
                public static class Updated
                {
                    public static readonly string Name = "Updated";
                    public static readonly int Value = 2;
                }

                public static class Rejected
                {
                    public static readonly string Name = "Rejected";
                    public static readonly int Value = 3;
                }
                public static class Approved
                {
                    public static readonly string Name = "Approved";
                    public static readonly int Value = 4;
                }
                public static class Payment
                {
                    public static readonly string Name = "Payment";
                    public static readonly int Value = 5;
                }
                public static class ChangeStatus
                {
                    public static readonly string Name = "Change Status";
                    public static readonly int Value = 6;
                }
                public static class OwnerApproved
                {
                    public static readonly string Name = "Owner Approved";
                    public static readonly int Value = 7;
                }
            }

        }

        public static class ResultManagement
        {
            public static readonly string Name = "Result Management";
            public static readonly int Value = 31;
            
            public static class EntryValidation
            {
                public static readonly string Name = "Entry Validation";
                public static readonly int Value = 0;
                
                public static class ImportMembers
                {
                    public static readonly string Name = "Import Members";
                    public static readonly int Value = 1;
                }
                
                public static class ReValidated
                {
                    public static readonly string Name = "ReValidated";
                    public static readonly int Value = 2;
                }
                
                public static class Deleted
                {
                    public static readonly string Name = "Deleted";
                    public static readonly int Value = 3;
                }
                
                public static class UpdatedFileStatus
                {
                    public static readonly string Name = "Updated File Status";
                    public static readonly int Value = 4;
                }
                
                public static class Downloaded
                {
                    public static readonly string Name = "Downloaded";
                    public static readonly int Value = 5;
                } 
            }
            
            public static class ResultUpload
            {
                public static readonly string Name = "Result Upload";
                public static readonly int Value = 1;
                
                public static class Uploaded //Action
                {
                    public static readonly string Name = "Uploaded";
                    public static readonly int Value = 1;
                }
                
                public static class Confirmed
                {
                    public static readonly string Name = "Confirmed";
                    public static readonly int Value = 2;
                }
                
                public static class Updated
                {
                    public static readonly string Name = "Updated";
                    public static readonly int Value = 3;
                }
                
                public static class Deleted
                {
                    public static readonly string Name = "Deleted";
                    public static readonly int Value = 4;
                }
                
                public static class UpdatedStatus
                {
                    public static readonly string Name = "UpdatedStatus";
                    public static readonly int Value = 5;
                }
                
                public static class ExceptionOccurred 
                {
                    public static readonly string Name = "Exception Occurred";
                    public static readonly int Value = 6;
                
                }
            }


            public static class ResultView
            {
                public static readonly string Name = "Result View";
                public static readonly int Value = 2;

                public static class Inserted
                {
                    public static readonly string Name = "Inserted";
                    public static readonly int Value = 1;
                }

                public static class Updated
                {
                    public static readonly string Name = "Updated";
                    public static readonly int Value = 2;
                }

                public static class Deleted
                {
                    public static readonly string Name = "Deleted";
                    public static readonly int Value = 3;
                }
            }

            public static class CompetitionRanking
            {
                public static readonly string Name = "CompetitionRanking";
                public static readonly int Value = 3;

                public static class Inserted
                {
                    public static readonly string Name = "Inserted";
                    public static readonly int Value = 1;
                }

                public static class Updated
                {
                    public static readonly string Name = "Updated";
                    public static readonly int Value = 2;
                }

                public static class Deleted
                {
                    public static readonly string Name = "Deleted";
                    public static readonly int Value = 3;
                }
            }

        }

        public static Dictionary<string, int> Category = new Dictionary<string, int>()
        {
            { "General Info",0 },
            { "System Error",1 },
            { "System Setting",2 },
            { "User Changed",3 },
            { "Family",4 },
            { "Security",5 },
            { "Membership",6 },
            { "Event,Course",7 },
            { "Club",8 },
            { "Reports",9 },
            { "Club Plus",10 },
            { "Team",11 },
            { "Chat",12},
            { "Email Management",13 },
            { "Field Management",14 },
            { "Legue Management",15 },
            { "Payment",16 },
            { "Bulk Import",17 },
            { "System Upgrade,Changed,Support",18 },
            { "Background Process",19 },
            { "Login",20 },
            { "Shopping Cart",21},
            { "Club Member",22},
            { "Finance", 23},
            { "Communication", 24},
            { "MFA", 25},
            { "User Merged", 26},
            { "Credential", 27},
            { "Class Booking", 28},
            { "Asset Management", 29},
        };
        public static Dictionary<string, Dictionary<string, int>> SubCategory = new Dictionary<string, Dictionary<string, int>>() {

            {
                "General Info",new Dictionary<string, int>()
                                 {
                                    { "Documents",0  },
                                    { "SystemStatistics",1  }
                                 }
            },
            {
                "System Setting",new Dictionary<string, int>()
                                 {
                                    { "General",0  },
                                    { "Optins",1  },
                                    { "Settings",2  },
                                    { "Eula",3  },
                                 }
            },
            {
                "User Changed",new Dictionary<string, int>()
                                 {
                                    { "General",0  },
                                    { "Basic Details",1  },
                                    { "Emergency Contact",2  },
                                    { "Additional Details",3  },
                                    { "Optins",4  },
                                    { "Payment",5  },
                                    { "Status Changed",6  },
                                    { "Course Booking",7  },
                                    { "Qualification",8 },
                                    { "Credential",9  },
                                    { "License",10  },
                                    { "Family",11  }
                                 }
            },
            { "Family",new Dictionary<string, int>(){ { "General", 0 } } },
            { "Legue Management",new Dictionary<string, int>(){ { "General", 0 } } },
            { "Bulk Import",new Dictionary<string, int>(){ { "General", 0 } } },
            {
                "Security",new Dictionary<string, int>()
                                 {
                                    { "General",0  },
                                    { "Role",1  },
                                    { "Group",2  },
                                    { "Password Reset Email",3  },
                                    { "Generate Password",4  },
                                    { "Password Changed",5 },
                                    { "Enable,Disable User",6  }

                                 }
            },
            { "User Merged",new Dictionary<string, int>(){ { "General", 0 } } }
            ,
            {
                "Membership",new Dictionary<string, int>()
                                 {
                                    { "General",0  },
                                    { "New Membership Created",1  },
                                    { "Membership Changed",2  },
                                    { "Membership Delete,Archived",3  },
                                    { "Membership Synched",4  }

                                 }
            }
            ,
            {
                "Event,Course",new Dictionary<string, int>()
                                 {
                                    { "General",0  },
                                    { "Details Changed",1  },
                                    { "Price Change",2  },
                                    { "Available Place change",3  },
                                    { "Course Booking",4  },
                                    { "End Date Change",5 },
                                    { "Start Date Change",6  },
                                    { "Event Contacts",7  },
                                    { "Event Settings",8  },
                                    { "Achivement",9  },
                                    { "Attachment",10  },
                                    { "Invitee",12  },
                                    { "Ticket",13  }

                                 }
            },
            {
                "Club",new Dictionary<string, int>()
                                 {
                                    { "General",0  },
                                    { "Details Changed",1  },
                                    { "Price Change",2  },
                                    { "Available Place change",3  },
                                    { "Course Booking",4  }

                                 }
            }
            ,
            {
                "Reports",new Dictionary<string, int>()
                                 {
                                    { "General",0  },
                                    { "Executed",1  },

                                 }
            },{
                "Club Plus",new Dictionary<string, int>()
                                 {
                                    { "General",0  },
                                    { "Central System Accessed",1  },
                                    { "Back From Central System",2  },

                                 }
            },{
                "Team",new Dictionary<string, int>()
                                 {
                                    { "General",0  },
                                    { "Join",1  },
                                    { "Leave",2  },

                                 }
            },{
                "Chat",new Dictionary<string, int>()
                                 {
                                    { "General",0  },
                                    { "State Change",1  },
                                    { "Copy",2  },

                                 }
            },{
                "Field Management",new Dictionary<string, int>()
                                 {
                                    { "General",0  },
                                    { "Profile",1  },
                                    { "Event",2  },
                                    { "Qualification",3 },
                                    { "Credential",4  },
                                    { "Synched From Mirror",5  }

                                 }
            },{
                "Payment",new Dictionary<string, int>()
                                 {
                                    { "General",0  },
                                    { "Purchase",1  },
                                    { "Invoice",2  },
                                    { "Refund",3  },
                                    { "Failed Payment",4  },
                                    { "Synched From Mirror",5  },
                                    { "Receipt Status Changed",6  },
                                    { "Payment User Changed",7  },
                                    { "Subscription",8  },
                                    { "Installment",9  },
                                    { "Mandate Setup",10  },
                                    { "Marchent Profile",11  },

                                 }
            },{
                "System Upgrade,Changed,Support",new Dictionary<string, int>()
                                 {
                                    { "General",0  },
                                    { "File Patched",1  },
                                    { "Rule",2  },
                                    { "Refund",3  },
                                    { "Setting",4  },
                                    { "Exchange Rate Changed",5  },
                                    { "Payment Config Changed",6  },
                                    { "DB Changed Manually",7  },
                                    { "Db Bulk Update",7  },
                                    { "Support Login",8  },
                                    { "Support Email Triggered",9  },
                                    { "Workbench Import",10  },
                                    { "Repository Import",11  },
                                    { "Scripting",12 },
                                    { "Form",13  },
                                    { "Workbench",14  },

                                 }
            },{
                "Background Process",new Dictionary<string, int>()
                                 {
                                    { "General",0  },
                                    { "Recurrent payment",1  },
                                    { "Email Send",2  },
                                    { "Scehdule Job",3  },
                                    { "Integration Exe",4  },
                                    { "Chat-View Mail Dump",5  }

                                 }
            },{
                "Login",new Dictionary<string, int>()
                                 {
                                    { "General",0  },
                                    { "Failed Login Attempt",1  },
                                    { "Successful Login",2  },
                                    { "Generate Login Token",3  },

                                 }
            },{
                "Shopping Cart",new Dictionary<string, int>()
                                 {
                                    { "General",0  },
                                    { "Purchase Rule Failed",1  },
                                    { "Quantity Changed",2  },
                                    { "Payment Failed",3  },

                                 }
            },{
                "Club Member",new Dictionary<string, int>()
                                 {
                                    { "General",0  },
                                    { "Join",1  },
                                    { "Leave",2  },

                                 }
            },
            {
                "Email Management",new Dictionary<string, int>()
                                 {
                                    { "General",0  },
                                    { "Attachment",1  },
                                    { "Report",2  },
                                    { "Email Template",3  },
                                    { "Scheme",4  }

                                 }
            },
            {
                "Finance",new Dictionary<string, int>()
                                 {
                                    { "General",0  },
                                    { "Payment Setup",1  },
                                    { "Account Setup",2  }
                                 }
            },
            {
                "Communication",new Dictionary<string, int>()
                                 {
                                    { "General",0  },
                                    { "Email",1  },
                                    { "Segment",2  }
                                 }
            },
            {
                "MFA",new Dictionary<string, int>()
                                 {
                                    { "General",0  },
                                    { "Authenticator App",1  },
                                    { "WhatsApp",2  }
                                 }
            },
             {
                "Class Booking",new Dictionary<string, int>()
                                 {
                                    { "General",0  },
                                    { "Details Changed",1  },
                                    { "Price Change",2  },
                                    { "Available Place change",3  },
                                    { "End Date Change",5 },
                                    { "Start Date Change",6  },
                                    { "MembershipChanges",7  },
                                    { "BookingContact",8  },
                                    { "Trial Changes",9  },
                                    { "Waitlist",10  },
                                    { "Cancel Booking",11  },

                                 }
            },
            {
                "Credential",new Dictionary<string, int>()
                                 {
                                    { "General",0  },
                                    { "Credential Created",1  },
                                    { "Credential Updated",1  }
                                 }
            },
            {
                "Asset Management",new Dictionary<string, int>()
                                 {
                                    { "General",0  },
                                    { "Details Changed",1  },
                                    { "Additional Details Changed",2  },
                                    { "Status Changed",3  },
                                    { "Asset Attachment",4  },
                                    { "Asset Credential",5  },
                                    { "Asset License",6  },
                                    { "Asset Lease",7  },
                                    { "Asset Lease Attachment",8  },

                                 }
            },
        };
        public static Dictionary<string, Dictionary<string, int>> Action = new Dictionary<string, Dictionary<string, int>>() {
            {
                "General Info|Documents|Credential",new Dictionary<string, int>()
                                 {
                                    { "Created",1  },
                                    { "Deleted",2  },
                                    { "Updated",3  }
                                 }
            },

            {
                "General Info|SystemStatistics",new Dictionary<string, int>()
                                 {
                                    { "SignIn",1  },
                                    { "SignOut",2  },
                                    { "Navigation",3  },
                                    { "ExecuteWidgetCommand",4  },
                                    { "MFA",5  },
                                    { "SignUpSuccess",6  },
                                    { "AttemptSignUp",7  }
                                 }
            },


            {
                "System Setting|Optins",new Dictionary<string, int>()
                                 {
                                    { "Ngb Optins-Add",1  },
                                    { "Ngb Optins-Update",2  },
                                    { "Ngb Optins-Delete",3  },
                                    { "Ngb Optins-Disable",4  },
                                    { "Club Optins-Add",5  },
                                    { "Club Optins-Update",6  },
                                    { "Club Optins-Delete",7  },
                                    { "Club Optins-Disable",8  },
                                    { "GoMembership Optins-Add",9  },
                                    { "GoMembership Optins-Update",10  },
                                    { "GoMembership Optins-Delete",11  },
                                    { "GoMembership Optins-Disable",12  },
                                 }
            },
            {
                "System Setting|Settings",new Dictionary<string, int>()
                                 {
                                    { "Ngb Settings-Update",1  },
                                    { "Club Settings-Update",2  },
                                    { "GoMembership Settings-Update",3  },
                                    { "User Settings-Update",4 },
                                 }
            },
            {
                "System Setting|Eula",new Dictionary<string, int>()
                                 {
                                    { "Ngb Eula-Add",1 },
                                    { "Ngb Eula-Update",2  },
                                    { "Ngb Eula-Delete",3  },
                                    { "Ngb Eula-Disable",4  },
                                    { "Club Eula-Add",5  },
                                    { "Club Eula-Update",6  },
                                    { "Club Eula-Delete",7  },
                                    { "Club Eula-Disable",8  },
                                    { "GoMembership Eula-Add",9  },
                                    { "GoMembership Eula-Update",10  },
                                    { "GoMembership Eula-Delete",11  },
                                    { "GoMembership Eula-Disable",12  },
                                 }
            },
            { "User Changed|General",new Dictionary<string, int>()
                {
                    { "Created", 1 } ,
                    { "Deleted", 2 }
                }
            },
            { "User Changed|Basic Details",new Dictionary<string, int>()
                {
                    { "Profile", 1 } ,
                    { "Email Changed", 2 } ,
                    { "Dob Changed", 3 } ,
                    { "Picture Changed", 4 } ,
                    { "Login Id Changed", 5 } ,
                    { "Active Status Changed", 6 } ,
                    { "Password Changed", 7 },
                    { "Merge User", 8 },
                    { "Gender Changed", 9 },
                    { "Phone Number Changed", 10 },
                    { "Name Changed", 11 },
                    { "Address Changed", 12 },

                }
            },
            { "User Changed|Emergency Contact",new Dictionary<string, int>()
                {
                    { "Emergency Contact-Add", 1 } ,
                    { "Emergency Contact-Update", 2 } ,
                    { "Emergency Contact-Delete", 3 } ,
                }
            },
            { "User Changed|Payment",new Dictionary<string, int>()
                {
                    { "Plan-Created", 1 } ,
                    { "Plan-Update", 2 } ,
                    { "Plan-Failed", 3 } ,
                    { "Plan-Cancelled", 4 } ,
                    { "Card Info Change", 5 } ,
                    { "Bank Info Change", 6 } ,
                    { "Mandate Change", 7 } ,
                    { "ScheduleDate Change", 8 } ,
                    { "Reactivate Schedule", 9 } ,
                    { "RecurringCustomer Created", 10 } ,
                    { "RecurringCustomer Updated", 11 } ,
                }
            },

            { "User Changed|Qualification",new Dictionary<string, int>()
                {
                 { "Qualification-Add", 1 },
                 { "Qualification-Expired", 2 },
                 { "Qualification-Approved", 3 },
                 { "Qualification-Rejected", 4 },
                 { "Qualification-Deleted", 5 },
                 { "Qualification-Updated", 6 }
                }
            },
            { "User Changed|Credential",new Dictionary<string, int>()
                {
                 { "Credential-Add", 1 },
                 { "Credential-Expired", 2 },
                 { "Credential-Approved", 3 },
                 { "Credential-Rejected", 4 },
                 { "Credential-Deleted", 5 },
                 { "Credential-Updated", 6 },
                 { "Credential-ExpireByApprove", 7 },
                 { "Credential-ExpireByCancel", 8 },
                 { "Credential-ExpireByExpire", 9 },
                 { "Credential-View", 10 }
                }
            },
            { "User Changed|License",new Dictionary<string, int>()
                {
                 { "License-Add", 1 },
                 { "License-Expired", 2 },
                 { "License-Deleted", 3 },
                 { "License-Updated", 4 }
                }
            },
            {
                "User Changed|Family",new Dictionary<string, int>()
                                 {
                                    { "Family-Add",1  },
                                    { "Family-Removed",2  },
                                    { "Family-Deleted",3  }
                                 }
            },
            {
                "User Changed|Optins",new Dictionary<string, int>()
                                 {
                                    { "Created",1  },
                                    { "Deleted",2  },
                                    { "Updated",3  }
                                 }
            },
            {
                "Security|Role",new Dictionary<string, int>()
                                 {
                                    { "Role-Add",1  },
                                    { "Role-Create",2  },
                                    { "Role-Update",3  },
                                    { "Role-Delete",4  },

                                 }
            },
            {
                "Security|Group",new Dictionary<string, int>()
                                 {
                                    { "Group-Add",1  },
                                    { "Group-Create",2  },
                                    { "Group-Update",3  },
                                    { "Group-Delete",4  },

                                 }
            },
            {
                "Membership|Membership Changed",new Dictionary<string, int>()
                                 {
                                    { "Basic Details"     ,1  },
                                    { "Price"     ,2  },
                                    { "Subcription"     ,3  },
                                    { "Installments"     ,4  },
                                    { "Tax"     ,5  },
                                    { "Benifits"     ,6  },
                                    { "Purchase Rule-Added"     ,7  },
                                    { "Purchase Rule-Updated"     ,8  },
                                    { "Purchase Rule-Deleted"     ,9  },
                                    { "Discount Rule-Added"     ,10  },
                                    { "Discount Rule-Updated"     ,11  },
                                    { "Discount Rule-Deleted"     ,12  },
                                    { "Surchange Rule-Added"     ,13  },
                                    { "Surchange Rule-Updated"     ,14  },
                                    { "Surchange Rule-Deleted"     ,15  },
                                    { "Additional-Optins"     ,16  },
                                    { "Additional-Credential"     ,17  },
                                    { "Additional-Qualification"     ,18  },
                                    { "Additional-Emergency Contact"     ,19  },
                                    { "Additional-Upsell Product"     ,20  },
                                    { "Additional-Fields"     ,21  },
                                    { "Section"     ,22  },
                                    { "MembershipStartDateEndDateSettings"     ,23  },
                                    { "dataCaptureItems Added"     ,24  },
                                    { "dataCaptureItems Updated"     ,25  },
                                    { "dataCaptureItems Deleted"     ,26  },

                                 }
            },
            {
                "Membership|Membership Synched",new Dictionary<string, int>()
                                 {
                                    { "Basic Details"     ,1  },
                                    { "Price"     ,2  },
                                    { "Subcription"     ,3  },
                                    { "Installments"     ,4  },
                                    { "Tax"     ,5  },
                                    { "Benifits"     ,6  },
                                    { "Purchase Rule-Added"     ,7  },
                                    { "Purchase Rule-Updated"     ,8  },
                                    { "Purchase Rule-Deleted"     ,9  },
                                    { "Discount Rule-Added"     ,10  },
                                    { "Discount Rule-Updated"     ,11  },
                                    { "Discount Rule-Deleted"     ,12  },
                                    { "Surchange Rule-Added"     ,13  },
                                    { "Surchange Rule-Updated"     ,14  },
                                    { "Surchange Rule-Deleted"     ,15  },
                                    { "Additional-Optins"     ,16  },
                                    { "Additional-Credential"     ,17  },
                                    { "Additional-Qualification"     ,18  },
                                    { "Additional-Emergency Contact"     ,19  },
                                    { "Additional-Upsell Product"     ,20  },
                                    { "Additional-Fields"     ,21  },
                                    { "Section"     ,22  },

                                 }
            },
            {
                "Event,Course|Event Contacts",new Dictionary<string, int>()
                                 {
                                    { "Event Contacts-Add",1  },
                                    { "Event Contacts-Remove",2  },
                                    { "Event Contacts-Update",3  },

                                 }
            },
            {
                "Event,Course|Event Settings",new Dictionary<string, int>()
                                 {
                                    { "Made Public",1  },
                                    { "Made Private",2 },
                                    { "Made Feature Event",3  },

                                 }
            } ,
            {
                "Event,Course|Achivement",new Dictionary<string, int>()
                                 {
                                    { "Credential-Add", 1 },
                                    { "Credential-Deleted",2 },
                                    { "Qualification-Add", 3 },
                                    { "Qualification-Deleted",4 },
                                 }
            } ,
            {
                "Event,Course|Attachment",new Dictionary<string, int>()
                                 {
                                    { "Attachment-Add",1  },
                                    { "Attachment-Remove",2  },

                                 }
            }
            , {
                "Event,Course|Invitee",new Dictionary<string, int>()
                                 {
                                    { "Invitee-Add",1  },
                                    { "Invitee-Remove",2  },

                                 }
            } , {
                "Event,Course|Course Booking",new Dictionary<string, int>()
                                 {
                                    { "Created",1  },
                                    { "Changed Status",2  }
                                 }
            },
            {
                "Event,Course|Ticket",new Dictionary<string, int>()
                                 {
                                    { "Ticket-Add"     ,1 },
                                    { "Ticket-Remove"     ,2  },
                                    { "Ticket-Update"     ,3  },
                                    { "Ticket-Available Place change"     ,4  },
                                    { "Ticket-Booking End Date Change"     , 5 },
                                    { "Ticket-Price"     ,6 },
                                    { "Ticket-Tax"     ,7  },
                                    { "Ticket-Purchase Rule-Added"     ,8  },
                                    { "Ticket-Purchase Rule-Updated"     ,9  },
                                    { "Ticket-Purchase Rule-Deleted"     ,10  },
                                    { "Ticket-Discount Rule-Added"     ,11  },
                                    { "Ticket-Discount Rule-Updated"     ,12  },
                                    { "Ticket-Discount Rule-Deleted"     ,13  },
                                    { "Ticket-Surchange Rule-Added"     ,14  },
                                    { "Ticket-Surchange Rule-Updated"     ,15  },
                                    { "Ticket-Surchange Rule-Deleted"     ,16  },
                                    { "Ticket-Additional-Optins"     ,17  },
                                    { "Ticket-Additional-Credential"     ,18  },
                                    { "Ticket-Additional-Qualification"     ,19  },
                                    { "Ticket-Additional-Emergency Contact"     ,20  },
                                    { "Ticket-Additional-Upsell Product"     ,21  },
                                    { "Ticket-Additional-Fields"     ,22  },
                                 }
            },{
                "Club|Details Changed",new Dictionary<string, int>()
                                 {
                                    { "Location Changed",1  },
                                    { "Lat-long Changed",2  },

                                 }
            }
            ,{
                "Club|Affiliation",new Dictionary<string, int>()
                                 {
                                   { "Affiliation-Added"     ,1  },
                                   { "Affiliation-Updated"     ,2  },
                                   { "Affiliation-Deleted"     ,3  },
                                 }
            }
           ,{
                "Field Management|Profile",new Dictionary<string, int>()
                                 {
                                    { "Profile Field-Add",1  },
                                    { "Profile Field-Update",2  },
                                    { "Profile Field-Delete",3  },

                                 }
            },{
                "Field Management|Event",new Dictionary<string, int>()
                                 {
                                    { "Event Field-Add",1  },
                                    { "Event Field-Update",2  },
                                    { "Event Field-Delete",3  },

                                 }
            },{
                "Field Management|Qualification",new Dictionary<string, int>()
                                 {
                                    { "Qualification Field-Add",1  },
                                    { "Qualification Field-Update",2  },
                                    { "Qualification Field-Delete",3  },

                                 }
            },{
                "Field Management|Credential",new Dictionary<string, int>()
                                 {
                                    { "Credential Field-Add",1  },
                                    { "Credential Field-Update",2  },
                                    { "Credential Field-Delete",3  },

                                 }
            } ,{
                "Payment|Invoice",new Dictionary<string, int>()
                                 {
                                    { "Invoice-Created",1  },
                                    { "Invoice-Updated",2  },
                                    { "Invoice-Deleted",3  },
                                    { "Invoice-Failed",4 },
                                    { "Invoice-Cancled",5  },

                                 }
            },{
                "Payment|Subscription",new Dictionary<string, int>()
                                 {
                                    { "Subscription-Created",1 },
                                    { "Subscription-Updated",2  },
                                    { "Subscription-Failed",3 },

                                 }
            },{
                "Payment|Installment",new Dictionary<string, int>()
                                 {
                                   { "Installment-Created",1  },
                                   { "Installment-Updated",2  },
                                   { "Installment-Failed",3 },

                                 }
            }
            ,{
                "Payment|Marchent Profile",new Dictionary<string, int>()
                                 {
                                   { "Marchent Profile-Created",1  },
                                   { "Marchent Profile-Updated",2  },
                                   { "Marchent Profile-Deleted",3  },

                                 }
            }
            ,
            {
                  "Club Member|General",new Dictionary<string, int>()
                                 {
                                    { "Created",1 },
                                    { "Updated",2 },
                                    { "Join",3  },
                                    { "Leave",4  },
                                    { "Roles Update",5  },
                                    { "Primary Club Update",6 },
                                    { "IsHidden Update",7 }
                                 }
            }
            ,
            {
                "Email Management|General",new Dictionary<string, int>()
                                 {
                                    { "Created",1  },
                                    { "Deleted",2  },
                                    { "Updated",3  },
                                    { "Active",4  },
                                    { "InActive",5  }
                                 }
            },
            {
                "Email Management|Attachment",new Dictionary<string, int>()
                                 {
                                    { "Added",1  },
                                    { "Deleted",2  }
                                 }
            },
            {
                "Email Management|Report",new Dictionary<string, int>()
                                 {
                                    { "Added",1  },
                                    { "Deleted",2  }
                                 }
            },
            {
                "Email Management|Email Template",new Dictionary<string, int>()
                                 {
                                    { "Created",1  },
                                    { "Deleted",2  },
                                    { "Updated",3  },
                                    { "Bulk Template Updated",4  }

                                 }
            }
            ,
            {
                "Email Management|Scheme",new Dictionary<string, int>()
                                 {
                                    { "Created",1  },
                                    { "Deleted",2  },
                                    { "Updated",3  },
                                    { "Active",4  },
                                    { "InActive",5  }

                                 }
            },
            {
                "Finance|General",new Dictionary<string, int>()
                                 {
                                    { "General",1  }
                                 }
            },
            {
                "Finance|Payment Setup",new Dictionary<string, int>()
                                 {
                                    { "Account Created",1  },
                                    { "Account Updated",2  },
                                    { "View Dashboard",3  },
                                    { "Payout Schedule Updated",4  },
                                    { "Export",5 },
                                    { "Account Deleted",6 },
                                 }
            },
            {
                "Finance|Account Setup",new Dictionary<string, int>()
                                 {
                                    { "BankDetails Updated", 1 },
                                    { "CardDetails Updated", 2 },
                                    { "BillingDetails Updated", 3 },
                                    { "Cancellation Request", 4 },
                                    { "Upgrade", 5 },
                                    { "Revert Cancellation Request", 6 }
                                 }
            },
            {
                "Communication|General",new Dictionary<string, int>()
                                 {
                                    { "General",1  },
                                    { "ExportStart",2  },
                                    { "ExportFinish",3  }
                                 }
            },
            {
                "Communication|Email",new Dictionary<string, int>()
                                 {
                                    { "Created", 1},
                                    { "Updated", 2},
                                    { "Deleted", 3},
                                    { "Export Report", 4}
                                 }
            },
            {
                "Communication|Segment",new Dictionary<string, int>()
                                 {
                                    { "Created", 1},
                                    { "Updated", 2},
                                    { "Deleted", 3},
                                    { "Changed Status", 4},
                                    { "Export Segment", 5}
                                 }
            },
            {
                "MFA|General",new Dictionary<string, int>()
                                 {
                                    { "Created", 1},
                                    { "Updated", 2},
                                    { "Deleted", 3},
                                    { "Login", 4}
                                 }
            },
            {
                "MFA|Authenticator App",new Dictionary<string, int>()
                                 {
                                    { "Created", 1},
                                    { "Deleted", 2},
                                    { "Login", 3}
                                 }
            },
            {
                "MFA|WhatsApp",new Dictionary<string, int>()
                                 {
                                    { "Created", 1},
                                    { "Updated", 2},
                                    { "Deleted", 3},
                                    { "Login", 4}
                                 }
            },
            { "User Merged|General",new Dictionary<string, int>()
                {
                    { "Created", 1 } ,
                    { "Deleted", 2 }
                }
            }
        };
        public static Tuple<int, int, int> GetCategorySubCategoryAction(string data)
        {
            int _category = 0, _subcategory = 0, _action = 0;
            var items = data.Split('|');
            if (Category.ContainsKey(items[0]))
            {
                _category = Category[items[0]];
                if (items.Length > 1)
                {
                    if (SubCategory.ContainsKey(items[0]) && SubCategory[items[0]].ContainsKey(items[1]))
                    {
                        _subcategory = SubCategory[items[0]][items[1]];
                    }
                }
                if (items.Length > 2)
                {
                    if (Action.ContainsKey(items[0] + "|" + items[1]) && Action[items[0] + "|" + items[1]].ContainsKey(items[2]))
                    {
                        _action = Action[items[0] + "|" + items[1]][items[2]];
                    }
                }
            }
            return new Tuple<int, int, int>(_category, _subcategory, _action);
        }
    }
}
