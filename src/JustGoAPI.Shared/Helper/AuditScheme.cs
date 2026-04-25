using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGoAPI.Shared.Helper
{
    public static class AuditScheme
    {
        public static Dictionary<string, int> Category = new ()
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
        };

        public static Dictionary<string, Dictionary<string, int>> SubCategory = new(){
            { "General Info", new()
                {
                    { "Documents", 0 },
                    { "SystemStatistics", 1 }
                }
            },
            { "System Setting", new()
                {
                    { "General", 0 },
                    { "Optins", 1 },
                    { "Settings", 2 },
                    { "Eula", 3 }
                }
            },
            { "User Changed", new()
                {
                    { "General", 0 },
                    { "Basic Details", 1 },
                    { "Emergency Contact", 2 },
                    { "Additional Details", 3 },
                    { "Optins", 4 },
                    { "Payment", 5 },
                    { "Status Changed", 6 },
                    { "Course Booking", 7 },
                    { "Qualification", 8 },
                    { "Credential", 9 },
                    { "License", 10 },
                    { "Family", 11 }
                }
            },
            { "Family", new() { { "General", 0 } } },
            { "League Management", new() { { "General", 0 } } },
            { "Bulk Import", new() { { "General", 0 } } },
            { "Security", new()
                {
                    { "General", 0 },
                    { "Role", 1 },
                    { "Group", 2 },
                    { "Password Reset Email", 3 },
                    { "Generate Password", 4 },
                    { "Password Changed", 5 },
                    { "Enable, Disable User", 6 }
                }
            },
            { "User Merged", new() { { "General", 0 } } },
            { "Membership", new()
                {
                    { "General", 0 },
                    { "New Membership Created", 1 },
                    { "Membership Changed", 2 },
                    { "Membership Delete, Archived", 3 },
                    { "Membership Synced", 4 }
                }
            },
            { "Event, Course", new()
                {
                    { "General", 0 },
                    { "Details Changed", 1 },
                    { "Price Change", 2 },
                    { "Available Place Change", 3 },
                    { "Course Booking", 4 },
                    { "End Date Change", 5 },
                    { "Start Date Change", 6 },
                    { "Event Contacts", 7 },
                    { "Event Settings", 8 },
                    { "Achievement", 9 },
                    { "Attachment", 10 },
                    { "Invitee", 12 },
                    { "Ticket", 13 }
                }
            },
            { "Club", new()
                {
                    { "General", 0 },
                    { "Details Changed", 1 },
                    { "Price Change", 2 },
                    { "Available Place Change", 3 },
                    { "Course Booking", 4 }
                }
            },
            { "Reports", new()
                {
                    { "General", 0 },
                    { "Executed", 1 }
                }
            },
            { "Club Plus", new()
                {
                    { "General", 0 },
                    { "Central System Accessed", 1 },
                    { "Back From Central System", 2 }
                }
            },
            { "Team", new()
                {
                    { "General", 0 },
                    { "Join", 1 },
                    { "Leave", 2 }
                }
            },
            { "Chat", new()
                {
                    { "General", 0 },
                    { "State Change", 1 },
                    { "Copy", 2 }
                }
            },
            { "Field Management", new()
                {
                    { "General", 0 },
                    { "Profile", 1 },
                    { "Event", 2 },
                    { "Qualification", 3 },
                    { "Credential", 4 },
                    { "Synced From Mirror", 5 }
                }
            },
            { "Payment", new()
                {
                    { "General", 0 },
                    { "Purchase", 1 },
                    { "Invoice", 2 },
                    { "Refund", 3 },
                    { "Failed Payment", 4 },
                    { "Synced From Mirror", 5 },
                    { "Receipt Status Changed", 6 },
                    { "Payment User Changed", 7 },
                    { "Subscription", 8 },
                    { "Installment", 9 },
                    { "Mandate Setup", 10 },
                    { "Merchant Profile", 11 }
                }
            },
            { "System Upgrade, Changed, Support", new()
                {
                    { "General", 0 },
                    { "File Patched", 1 },
                    { "Rule", 2 },
                    { "Refund", 3 },
                    { "Setting", 4 },
                    { "Exchange Rate Changed", 5 },
                    { "Payment Config Changed", 6 },
                    { "DB Changed Manually", 7 },
                    { "Db Bulk Update", 7 },
                    { "Support Login", 8 },
                    { "Support Email Triggered", 9 },
                    { "Workbench Import", 10 },
                    { "Repository Import", 11 },
                    { "Scripting", 12 },
                    { "Form", 13 },
                    { "Workbench", 14 }
                }
            },
            { "Background Process", new()
                {
                    { "General", 0 },
                    { "Recurrent Payment", 1 },
                    { "Email Send", 2 },
                    { "Schedule Job", 3 },
                    { "Integration Exe", 4 },
                    { "Chat-View Mail Dump", 5 }
                }
            },
            { "Login", new()
                {
                    { "General", 0 },
                    { "Failed Login Attempt", 1 },
                    { "Successful Login", 2 },
                    { "Generate Login Token", 3 }
                }
            },
            { "Shopping Cart", new()
                {
                    { "General", 0 },
                    { "Purchase Rule Failed", 1 },
                    { "Quantity Changed", 2 },
                    { "Payment Failed", 3 }
                }
            },
            { "Club Member", new()
                {
                    { "General", 0 },
                    { "Join", 1 },
                    { "Leave", 2 }
                }
            },
            { "Email Management", new()
                {
                    { "General", 0 },
                    { "Attachment", 1 },
                    { "Report", 2 },
                    { "Email Template", 3 },
                    { "Scheme", 4 }
                }
            },
            { "Finance", new()
                {
                    { "General", 0 },
                    { "Payment Setup", 1 },
                    { "Account Setup", 2 }
                }
            },
            { "Communication", new()
                {
                    { "General", 0 },
                    { "Email", 1 },
                    { "Segment", 2 }
                }
            },
            { "MFA", new()
                {
                    { "General", 0 },
                    { "Authenticator App", 1 },
                    { "WhatsApp", 2 }
                }
            },
            { "Class Booking", new()
                {
                    { "General", 0 },
                    { "Details Changed", 1 },
                    { "Price Change", 2 },
                    { "Available Place Change", 3 },
                    { "End Date Change", 5 },
                    { "Start Date Change", 6 },
                    { "Membership Changes", 7 },
                    { "Booking Contact", 8 },
                    { "Trial Changes", 9 }
                }
            },
            { "Credential", new()
                {
                    { "General", 0 },
                    { "Credential Created", 1 },
                    { "Credential Updated", 1 }
                }
            }
        };

        public static Dictionary<string, Dictionary<string, int>> Action = new (){
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
            var items = data.Split('|');
            if (items.Length == 0 || !Category.ContainsKey(items[0]))
                return new Tuple<int, int, int>(0, 0, 0);

            int _category = Category[items[0]];
            int _subcategory = 0, _action = 0;

            if (items.Length > 1 && SubCategory.ContainsKey(items[0]) && SubCategory[items[0]].ContainsKey(items[1]))
            {
                _subcategory = SubCategory[items[0]][items[1]];
            }

            if (items.Length > 2 && Action.ContainsKey(items[0] + "|" + items[1]) && Action[items[0] + "|" + items[1]].ContainsKey(items[2]))
            {
                _action = Action[items[0] + "|" + items[1]][items[2]];
            }

            return new Tuple<int, int, int>(_category, _subcategory, _action);
        }

    }
}
