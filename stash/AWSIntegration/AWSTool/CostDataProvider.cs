using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWSTool
{
    public enum VMSize
    {
        None,
        Small,
        Medium,
        Large,
        ExtraLarge,
        ExtraLargeSecond,
        TwoExtraLargeSecond,
        Micro,
        ExtraLargeMem,
        TwoExtraLargeMem,
        FourExtraLargeMem,
        MediumCompute,
        ExtraLargeCompute,
        EightExtraLargeCluster,
        EightExtraLargeClusterMem,
        FourExtraLargeGPU,
        FourExtraLargeIO,
        EightExtraLargeStorage,
    }

    public enum Region
    {
        None,
        USEast,
        USWest_Oregon,
        USWest_California,
        EU,
        Asia_Singapore,
        Asia_Tokyo,
        Asia_Sydney,
        SouthAmerica
    }

    public static class CostDataProvider
    {
        public static decimal InstanceCost(Region region, string vmType)
        {
            VMSize size;
            if (VmTypeToSize.TryGetValue(vmType, out size))
            {
                return InstanceCost(region, size);
            }

            throw new ArgumentException(string.Format("Bad vm type: {0}", vmType));
        }

        public static decimal InstanceCost(Region region, VMSize vmSize)
        {
            IDictionary<VMSize, decimal> costDict;
            if (VmCosts.TryGetValue(region, out costDict))
            {
                decimal cost;
                if (costDict.TryGetValue(vmSize, out cost))
                {
                    return cost;
                }
            }

            throw new ArgumentException("no cost found for given arguments");
        }

        public static IDictionary<string, VMSize> VmTypeToSize = new Dictionary<string, VMSize>()
            {
                {"m1.small", VMSize.Small},
                {"m1.medium", VMSize.Medium},
                {"m1.large", VMSize.Large},
                {"m1.xlarge", VMSize.ExtraLarge},
                {"m3.xlarge", VMSize.ExtraLargeSecond},
                {"m3.2xlarge", VMSize.TwoExtraLargeSecond},
                {"c1.medium", VMSize.MediumCompute},
                {"c1.xlarge", VMSize.ExtraLargeCompute},
                {"cc2.8xlarge", VMSize.EightExtraLargeCluster},
                {"m2.xlarge", VMSize.ExtraLargeMem},
                {"m2.2xlarge", VMSize.TwoExtraLargeMem},
                {"m2.4xlarge", VMSize.FourExtraLargeMem},
                {"cr1.8xlarge", VMSize.EightExtraLargeClusterMem},
                {"hi1.4xlarge", VMSize.FourExtraLargeIO},
                {"hs1.8xlarge", VMSize.EightExtraLargeStorage},
                {"t1.micro", VMSize.Micro},
                {"cg1.4xlarge", VMSize.FourExtraLargeGPU},
            };

        public static IDictionary<Region, IDictionary<VMSize, decimal>> VmCosts = new Dictionary
            <Region, IDictionary<VMSize, decimal>>()
            {
                {
                    Region.USEast, new Dictionary<VMSize, decimal>()
                        {
                            {VMSize.Small, 0.06M},
                            {VMSize.Medium, 0.12M},
                            {VMSize.Large, 0.24M},
                            {VMSize.ExtraLarge, 0.48M},
                            {VMSize.ExtraLargeSecond, 0.5M},
                            {VMSize.TwoExtraLargeSecond, 1M},
                            {VMSize.Micro, 0.02M},
                            {VMSize.ExtraLargeMem, 0.41M},
                            {VMSize.TwoExtraLargeMem, 0.82M},
                            {VMSize.FourExtraLargeMem, 1.64M},
                            {VMSize.MediumCompute, 0.145M},
                            {VMSize.ExtraLargeCompute, 0.58M},
                            {VMSize.EightExtraLargeCluster, 2.4M},
                            {VMSize.EightExtraLargeClusterMem, 3.5M},
                            {VMSize.FourExtraLargeGPU, 2.1M},
                            {VMSize.FourExtraLargeIO, 3.1M},
                            {VMSize.EightExtraLargeStorage, 4.6M},
                        }
                },
                {
                    Region.USWest_Oregon, new Dictionary<VMSize, decimal>()
                        {
                            {VMSize.Small, 0.06M},
                            {VMSize.Medium, 0.12M},
                            {VMSize.Large, 0.24M},
                            {VMSize.ExtraLarge, 0.48M},
                            {VMSize.ExtraLargeSecond, 0.5M},
                            {VMSize.TwoExtraLargeSecond, 1M},
                            {VMSize.Micro, 0.02M},
                            {VMSize.ExtraLargeMem, 0.41M},
                            {VMSize.TwoExtraLargeMem, 0.82M},
                            {VMSize.FourExtraLargeMem, 1.64M},
                            {VMSize.MediumCompute, 0.145M},
                            {VMSize.ExtraLargeCompute, 0.58M},
                            {VMSize.EightExtraLargeCluster, 2.4M},
                            {VMSize.EightExtraLargeClusterMem, 3.5M},
                            {VMSize.FourExtraLargeIO, 3.1M},
                            {VMSize.EightExtraLargeStorage, 4.6M},
                        }
                },
                {
                    Region.USWest_California, new Dictionary<VMSize, decimal>()
                        {
                            {VMSize.Small, 0.065M},
                            {VMSize.Medium, 0.13M},
                            {VMSize.Large, 0.26M},
                            {VMSize.ExtraLarge, 0.52M},
                            {VMSize.ExtraLargeSecond, 0.55M},
                            {VMSize.TwoExtraLargeSecond, 1.1M},
                            {VMSize.Micro, 0.025M},
                            {VMSize.ExtraLargeMem, 0.46M},
                            {VMSize.TwoExtraLargeMem, 0.92M},
                            {VMSize.FourExtraLargeMem, 1.84M},
                            {VMSize.MediumCompute, 0.165M},
                            {VMSize.ExtraLargeCompute, 0.66M},
                        }
                },
                {
                    Region.EU, new Dictionary<VMSize, decimal>()
                        {
                            {VMSize.Small, 0.065M},
                            {VMSize.Medium, 0.13M},
                            {VMSize.Large, 0.26M},
                            {VMSize.ExtraLarge, 0.52M},
                        }
                },
                {
                    Region.Asia_Singapore, new Dictionary<VMSize, decimal>()
                        {
                            {VMSize.Small, 0.08M},
                            {VMSize.Medium, 0.16M},
                            {VMSize.Large, 0.32M},
                            {VMSize.ExtraLarge, 0.64M},
                        }
                },
                {
                    Region.Asia_Tokyo, new Dictionary<VMSize, decimal>()
                        {
                            {VMSize.Small, 0.088M},
                            {VMSize.Medium, 0.175M},
                            {VMSize.Large, 0.35M},
                            {VMSize.ExtraLarge, 0.7M},
                        }
                },
                {
                    Region.Asia_Sydney, new Dictionary<VMSize, decimal>()
                        {
                            {VMSize.Small, 0.08M},
                            {VMSize.Medium, 0.16M},
                            {VMSize.Large, 0.32M},
                            {VMSize.ExtraLarge, 0.64M},
                        }
                },
                {
                    Region.SouthAmerica, new Dictionary<VMSize, decimal>()
                        {
                            {VMSize.Small, 0.08M},
                            {VMSize.Medium, 0.16M},
                            {VMSize.Large, 0.32M},
                            {VMSize.ExtraLarge, 0.64M},
                        }
                },
            };
    }
}
