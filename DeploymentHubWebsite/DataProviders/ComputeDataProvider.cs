using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWSTool
{
    public class ComputeDataProvider
    {
        public IEnumerable<EC2Instance> FindInstances(string keyId, string key, bool loadImages = true)
        {
            var regions = new[]
            {
                RegionEndpoint.USEast1,
                RegionEndpoint.USWest1,
                RegionEndpoint.USWest2
            };

            return regions.SelectMany(region => this.FindInstances(keyId, key, region, loadImages));
        }

        private IEnumerable<EC2Instance> FindInstances(string keyId, string key, RegionEndpoint region, bool loadImages = true)
        {
            var client = AWSClientFactory.CreateAmazonEC2Client(
                awsAccessKey: keyId,
                awsSecretAccessKey: key,
                region: region);

            var request = new DescribeInstancesRequest();

            var result = client.DescribeInstances(request);

            if (result == null || result.DescribeInstancesResult == null)
            {
                yield break;
            }

            foreach (var reservation in result.DescribeInstancesResult.Reservation)
            {
                // reservation group == security group
                var runningInstance = reservation.RunningInstance.FirstOrDefault();

                if (runningInstance != null)
                {
                    EC2Instance instance = new EC2Instance();

                    // static data
                    instance.InstanceId = runningInstance.InstanceId;
                    instance.State = runningInstance.InstanceState.Name;
                    instance.Type = runningInstance.InstanceType;
                    instance.Location = runningInstance.Placement.AvailabilityZone;
                    instance.LaunchTime = runningInstance.LaunchTime;
                    instance.IsRunning = reservation.IsSetRunningInstance();
                    instance.Tags = runningInstance.Tag
                        .Where(tag => tag != null && tag.Key != null)
                        .ToDictionary(tag => tag.Key, tag => tag.Value);

                    // cost info
                    var costRegion = region == RegionEndpoint.USEast1 ? Region.USEast :
                        region == RegionEndpoint.USWest1 ? Region.USWest_Oregon :
                        region == RegionEndpoint.USWest2 ? Region.USWest_California :
                        Region.USEast;

                    instance.HourlyCost = CostDataProvider.InstanceCost(
                        region: costRegion,
                        vmType: instance.Type);

                    // connection info
                    instance.KeyName = runningInstance.KeyName;
                    instance.PublicHostName = runningInstance.PublicDnsName;
                    instance.IpAddress = runningInstance.IpAddress;
                    instance.PrivateIpAddress = runningInstance.PrivateIpAddress;

                    // cost info
                    instance.IsEbsEnabled = runningInstance.RootDeviceType != null &&
                        runningInstance.RootDeviceType.Equals("ebs", StringComparison.InvariantCultureIgnoreCase) &&
                        runningInstance.BlockDeviceMapping != null &&
                        runningInstance.BlockDeviceMapping.Any();
                    instance.IsElbEnabled = true;

                    // image info
                    instance.ImageId = runningInstance.ImageId;

                    if (loadImages)
                    {
                        var imageRequest = new DescribeImagesRequest().WithImageId(instance.ImageId);
                        var imageResult = client.DescribeImages(imageRequest);

                        if (imageResult != null &&
                            imageResult.DescribeImagesResult != null &&
                            imageResult.DescribeImagesResult.Image != null)
                        {
                            var imageDetails = imageResult.DescribeImagesResult.Image.FirstOrDefault();

                            if (imageDetails != null)
                            {
                                instance.ImageDescription = imageDetails.Description;
                                instance.ImageName = imageDetails.Name;
                                instance.IsWindows = imageDetails.Description.IndexOf("Windows", StringComparison.InvariantCultureIgnoreCase) >= 0;
                            }
                        }
                    }

                    yield return instance;
                }
            }
        }
    }
}
