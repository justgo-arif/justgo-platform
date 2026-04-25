using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MobileApps.Domain.Entities.V2.Members;

namespace MobileApps.Domain.Entities.V2.Classes
{
    public class EligibilityMapping
    {
        public static Dictionary<string, object> CombinedEligibility(PersonalInfo personalInfo, List<RuleModel> ruleModels, PaymentStatusModel payment = null, bool membership = true)
        {
            var data = new Dictionary<string, object>();
            data.Add("GenderRestriction", "");
            data.Add("AgeRestriction", "");
            data.Add("Membership", "");
            data.Add("Payment", "");

            bool genderOk = false;
            bool ageOk = false;

            //int memberAge = CalculateAge(personalInfo.DateOfBirth);
            //gender and age rules
            if (ruleModels.Count > 0)
            {
                foreach (var itemRules in ruleModels)
                {

                    foreach (var item in itemRules?.RuleExpression)
                    {
                        if (item != null && item.RuleName == "GenericGenderRule" && !genderOk)
                        {
                            if (item is GenericGenderRule genderRule)
                            {
                                string formattedString = genderRule.Gender.Replace("__", " ");
                                if (formattedString.Contains(personalInfo.Gender.ToString()))
                                {
                                    genderOk = true;
                                }
                            }
                        }

                        if (item != null && item.RuleName == "GenericAgeRule")
                        {
                            if (item is GenericAgeRule ageRule)
                            {

                                data["AgeRestriction"] = ageRule.Name.ToString();
                            }
                        }
                    }

                    // Only set "" if any passes, otherwise "Negative"
                    data["GenderRestriction"] = genderOk ? "" : "Gender Restriction";

                }

            }

            //payment rules
            if (payment != null && payment.PaymentStatus.ToLower() == "due")
            {
                data["Payment"] = "Payment";
            }

            //membership rules
            if (!membership) data["Membership"] = "Membership";
            return data;
        }
        //private static int CalculateAge(DateTime dateOfBirth)
        //{
        //    DateTime today = DateTime.Today;
        //    int age = today.Year - dateOfBirth.Year;

        //    if (dateOfBirth.Date > today.AddYears(-age))
        //        age--;

        //    return age;
        //}
    }
}
