using System.Collections.Generic;
using System.Linq;

namespace TagSync.Services
{
    public static class TagService
    {
        /// <summary>
        /// Returns the required tags the resource group currently has
        /// </summary>
        /// <returns>List of required tags (keys and values) the resource group currently has</returns>
        /// <param name="rgTags">Restource group tags</param>
        /// <param name="requiredTags">Required tags</param>
        public static Dictionary<string, string> GetRequiredTags(Dictionary<string, string> rgTags, IEnumerable<string> requiredTags )
        {
            var matchingTagsEnum = rgTags.Where(rg => requiredTags.Contains(rg.Key));

            if (matchingTagsEnum != null && matchingTagsEnum.Count() > 0)
                return matchingTagsEnum.ToDictionary(x => x.Key, x => x.Value);

            return new Dictionary<string, string>();
        }

        /// <summary>
        ///  Updates resources tags based on values from the resource group
        /// </summary>
        /// <returns>All resrouce tags with new or updated tags from the update tags that are passed in</returns>
        /// <param name="resourceTags">Resource tags.</param>
        /// <param name="updateTags">Required tags.</param>
        public static Dictionary<string, string> GetTagUpdates(Dictionary<string, string> resourceTags, Dictionary<string, string> updateTags)
        {
            bool tagUpadateRequired = false;

            foreach (var requiredTag in updateTags)
            {
                if (resourceTags == null) // resource does not have any tags. Set to the RG required tags and exit.
                {
                    return updateTags;
                }
                if (resourceTags.ContainsKey(requiredTag.Key)) // resource has a matching rquired RG tag.
                {
                    if (resourceTags[requiredTag.Key] != requiredTag.Value) // update resource tag value if it doesn't match the current RG tag
                    {
                        resourceTags[requiredTag.Key] = requiredTag.Value;
                        tagUpadateRequired = true;
                    }
                }
                else
                {
                    resourceTags.Add(requiredTag.Key, requiredTag.Value);
                    tagUpadateRequired = true;
                }
            }

            if (tagUpadateRequired)
            {
                return resourceTags;
            }
            else
            {
                return new Dictionary<string, string>();
            }

        }

    }
}
