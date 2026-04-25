using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustGoAPI.Shared.Helper
{
    public static class AttendanceCountShared
    {
        public static IList<IDictionary<string, object>> MergeStatusLists(IEnumerable<object> eventCount)
        {
            var mergedList = eventCount
                .Cast<IDictionary<string, object>>() // Ensure each object is treated as a dictionary
                .GroupBy(dict => dict["StatusName"].ToString())
                .Select(group => new Dictionary<string, object>
                {
                    { "StatusName", group.Key },
                    { "StatusCount", group.Sum(d => Convert.ToInt32(d["StatusCount"])) }
                } as IDictionary<string, object>).ToList();

            // Separate specified statuses for total count calculation
            var specificStatuses = new HashSet<string> { "Away", "Injured", "Not Showed" };

            // Calculate total count of specific statuses
            var totalAbsentCount = mergedList.Where(dict => specificStatuses.Contains(dict["StatusName"].ToString())).Sum(dict => Convert.ToInt32(dict["StatusCount"]));

            // Separate other statuses
            var otherStatusList = mergedList
                .Where(dict => !specificStatuses.Contains(dict["StatusName"].ToString()))
                .ToList();

            // Add the calculated total count for specified statuses as a new entry
            var totalCountDict = new Dictionary<string, object>
                 {
                     { "StatusName", "Absent" },
                     { "StatusCount",totalAbsentCount }
                 };

            // Add the total count entry to the final list
            otherStatusList.Add(totalCountDict);

            // total count 
            int totalCount = mergedList.Sum(dict => Convert.ToInt32(dict["StatusCount"]));
            var totalCountStatus = new Dictionary<string, object>
            {
                { "StatusName", "Total" },
                { "StatusCount", totalCount }
            };
            otherStatusList.Add(totalCountStatus);


            return otherStatusList;


        }

        public static IList<IDictionary<string, object>> MergeClassStatusLists(IEnumerable<object> eventCount)
        {
            var mergedList = eventCount
                .Cast<IDictionary<string, object>>() // Ensure each object is treated as a dictionary
                .GroupBy(dict => dict["StatusName"].ToString())
                .Select(group => new Dictionary<string, object>
                {
                    { "StatusName", group.Key },
                    { "StatusCount", group.Sum(d => Convert.ToInt32(d["StatusCount"])) }
                } as IDictionary<string, object>).ToList();

            // Separate specified statuses for total count calculation
            var specificStatuses = new HashSet<string> { "Away", "Injured"};

            // Calculate total count of specific statuses
            var totalAbsentCount = mergedList
                .Where(dict => specificStatuses.Contains(dict["StatusName"].ToString()))
                .Sum(dict => Convert.ToInt32(dict["StatusCount"]));

            // Separate other statuses
            var otherStatusList = mergedList
                .Where(dict => !specificStatuses.Contains(dict["StatusName"].ToString()))
                .ToList();

            // Add the total count entry for absent-related statuses
            var totalCountDict = new Dictionary<string, object>
            {
                { "StatusName", "Absent" },
                { "StatusCount", totalAbsentCount }
            };
            otherStatusList.Add(totalCountDict);

            // Add TrialCount
            var trialCount = eventCount
                .Cast<IDictionary<string, object>>()
                .Where(d => d.ContainsKey("BookingType") && d["BookingType"]?.ToString().ToLower() == "trial")
                .Sum(d => Convert.ToInt32(d["StatusCount"]));

            var trialCountDict = new Dictionary<string, object>
                {
                    { "StatusName", "TrialCount" },
                    { "StatusCount", trialCount }
                };
            otherStatusList.Add(trialCountDict);

            // Add total count
            int totalCount = mergedList.Sum(dict => Convert.ToInt32(dict["StatusCount"]));
            var totalCountStatus = new Dictionary<string, object>
                {
                    { "StatusName", "Total" },
                    { "StatusCount", totalCount }
                };
            otherStatusList.Add(totalCountStatus);

            return otherStatusList;
        }

        public static IList<IDictionary<string, object>> MergeClubBookingStatusLists(IEnumerable<object> eventCount,IEnumerable<object> recurringEventCount)
        {
            // Convert raw results to dictionaries
            var mergedList = eventCount.Cast<IDictionary<string, object>>()
            .Concat(recurringEventCount.Cast<IDictionary<string, object>>())
            .GroupBy(d => d["StatusName"]?.ToString() ?? "Unknown")
            .Select(g => new Dictionary<string, object>
            {
                { "StatusName", g.Key },
                { "StatusCount", g.Sum(d => Convert.ToInt32(d["StatusCount"])) }
            } as IDictionary<string, object>).ToList();

            // Statuses to group under "Absent"
            var absentStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase){"Away", "Injured", "No Show" };

            // Calculate total for "Absent"
            var absentCount = mergedList
            .Where(d => absentStatuses.Contains(d["StatusName"].ToString()))
            .Sum(d => Convert.ToInt32(d["StatusCount"]));


            // Filter out the absent statuses
            var finalList = mergedList
            .Where(d => !absentStatuses.Contains(d["StatusName"].ToString())).ToList();

            // Add "Absent" row if needed
            if (absentCount > 0)
            {
                finalList.Add(new Dictionary<string, object>{{ "StatusName", "Absent" },{ "StatusCount", absentCount }});
            }

            // Add "Total" row
            var totalCount = mergedList.Sum(d => Convert.ToInt32(d["StatusCount"]));
            finalList.Add(new Dictionary<string, object>
            {
                { "StatusName", "Total" },
                { "StatusCount", totalCount }
            });

            return finalList;
        }

    }
}
