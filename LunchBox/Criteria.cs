﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace LunchBox
{
    public class Criteria
    {
        public List<string> Attendees { get; set; }
        public bool? HasTimeRestrictions { get; set; }
        public TimeSpan? LunchDuration { get; set; }
        public List<string> Cuisines { get; set; }
        public bool HasEnoughForRecommendation
        {
            get
            {
                return Attendees != null && Attendees.Count() > 0;
            }
        }

        public Criteria()
        {
            Attendees = new List<string>();
            Cuisines = new List<string>();
        }
    }
}