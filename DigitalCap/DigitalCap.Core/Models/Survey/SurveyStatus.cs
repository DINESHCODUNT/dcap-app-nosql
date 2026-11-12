using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalCap.Core.Models.Survey
{
    public class SurveyStatus
    {
        public int Vessel_ID { get; set; }
        public int IMONum { get; set; }
        public DateTime? ClassSurveyDate { get; set; }
        public DateTime? DryDockDate { get; set; }
        public DateTime? SpecialContinuousMachineryDate { get; set; }
        public DateTime? BoilerDate { get; set; }
        public DateTime? TailshaftDate { get; set; }
        public DateTime? SpecialContinuousHull1 { get; set; }
        public DateTime? SpecialContinuousHull2 { get; set; }
        public DateTime? SpecialContinuousHull3 { get; set; }
        public DateTime? SpecialContinuousHull4 { get; set; }
        public DateTime? SpecialContinuousHull5 { get; set; }
        public DateTime? SpecialContinuousHull6 { get; set; }
        public DateTime? SpecialContinuousHull7 { get; set; }
        public DateTime? SpecialContinuousHull8 { get; set; }
        public DateTime? SpecialContinuousHull9 { get; set; }
        public DateTime? SpecialContinuousHull10 { get; set; }
        public DateTime? SpecialContinuousHull11 { get; set; }
        public DateTime? SpecialContinuousHull12 { get; set; }
        public DateTime? SpecialContinuousHull13 { get; set; }
        public DateTime? SpecialContinuousHull14 { get; set; }
        public DateTime? SpecialContinuousHull15 { get; set; }

        public int? SpecialHullNo { get; set; }
        public DateTime? GetHullSurveyDate()
        {
            return GetSurvey().Value;
        }
        public int GetHullSurveyNumber()
        {
            return GetSurvey().Key;
        }
        private KeyValuePair<int, DateTime?> GetSurvey()
        {
            if (SpecialHullNo == 1)
            {
                return new KeyValuePair<int, DateTime?>(1, SpecialContinuousHull1);
            }
            else if (SpecialHullNo == 2)
            {
                return new KeyValuePair<int, DateTime?>(2, SpecialContinuousHull2);
            }
            else if (SpecialHullNo == 3)
            {
                return new KeyValuePair<int, DateTime?>(3, SpecialContinuousHull3);
            }
            else if (SpecialHullNo == 4)
            {
                return new KeyValuePair<int, DateTime?>(4, SpecialContinuousHull4);
            }
            else if (SpecialHullNo == 5)
            {
                return new KeyValuePair<int, DateTime?>(5, SpecialContinuousHull5);
            }
            else if (SpecialHullNo == 6)
            {
                return new KeyValuePair<int, DateTime?>(6, SpecialContinuousHull6);
            }
            else if (SpecialHullNo == 7)
            {
                return new KeyValuePair<int, DateTime?>(7, SpecialContinuousHull7);
            }
            else if (SpecialHullNo == 8)
            {
                return new KeyValuePair<int, DateTime?>(8, SpecialContinuousHull8);
            }
            else if (SpecialHullNo == 9)
            {
                return new KeyValuePair<int, DateTime?>(9, SpecialContinuousHull9);
            }
            else if (SpecialHullNo == 10)
            {
                return new KeyValuePair<int, DateTime?>(10, SpecialContinuousHull10);
            }
            else if (SpecialHullNo == 11)
            {
                return new KeyValuePair<int, DateTime?>(11, SpecialContinuousHull11);
            }
            else if (SpecialHullNo == 12)
            {
                return new KeyValuePair<int, DateTime?>(12, SpecialContinuousHull12);
            }
            else if (SpecialHullNo == 13)
            {
                return new KeyValuePair<int, DateTime?>(13, SpecialContinuousHull13);
            }
            else if (SpecialHullNo == 14)
            {
                return new KeyValuePair<int, DateTime?>(14, SpecialContinuousHull14);
            }
            else if (SpecialHullNo == 15)
            {
                return new KeyValuePair<int, DateTime?>(15, SpecialContinuousHull15);
            }
            return new KeyValuePair<int, DateTime?>(-1, null);
        }
    }
}
