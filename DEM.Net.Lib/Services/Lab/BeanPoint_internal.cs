﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DEM.Net.Lib.Services.Lab
{
    public class BeanPoint_internal
    {
        private static int _dernierId = 0;

        public int p00_id { get; }
        //
        private string p01_hCodeGeogP;
        public string p01_hCodeGeog
        {
            set
            {
                p01_hCodeGeogP=FLabServices.createUtilitaires().GethCodeGeogPoint(p10_coord);
            }
            get
            {
                if(p01_hCodeGeogP=="")
                {
                    p01_hCodeGeogP = FLabServices.createUtilitaires().GethCodeGeogPoint(p10_coord);
                }
                return p01_hCodeGeogP;
            }
        }
        //
        public double[] p10_coord { get; set; }
        public int p11_srid { get; set; }
        //
        public bool p21_estPointFacette_vf { get; set; }
        public bool p22_estPointInclus_vf { get; set; }
        //
        public double p31_ecartAbsAuPlanCourant { get; set; }
        //
        public BeanPoint_internal(double[] p_coord, int p_srid)
        {
            p00_id = _dernierId++;
            p10_coord = p_coord;
           p01_hCodeGeog= FLabServices.createUtilitaires().GethCodeGeogPoint(p10_coord);
            p11_srid = p_srid;
        }
        public BeanPoint_internal(double p_x, double p_y, double p_z, int p_srid)
        {
            p00_id = _dernierId++;
            p10_coord=new double[3] { p_x, p_y, p_z };
            p01_hCodeGeog = FLabServices.createUtilitaires().GethCodeGeogPoint(p10_coord);
            p11_srid = p_srid;
        }

    }
}