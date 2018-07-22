using System;
using Xunit;
using TagSync.Services;
using System.Collections;
using System.Collections.Generic;

namespace TagSync.Tests
{
    public class TagServiceTests
    {
        [Fact]
        public void RgHasNoRequiredTags()
        {
            // Arrange - Resrouce group has one tag and no required ones
            var requiredTags = new List<string>() { "tag1", "tag2", "tag3" };
            Dictionary<string, string> rgTags = new Dictionary<string, string>();
            rgTags.Add("tag4", "value4");

            // Act
            var result = TagService.GetRequiredTags(rgTags, requiredTags);

            // Assert
            Dictionary<string, string> expectedResult = new Dictionary<string, string>();
            Assert.Equal<Dictionary<string, string>>(result, expectedResult);
        }

        [Fact]
        public void RgHasOneRequiredTag()
        {
            // Arrange - Resrouce group has multiple tags but only includes one defined required tag
            var requiredTags = new List<string>() { "tag1", "tag2" };
            Dictionary<string, string> rgTags = new Dictionary<string, string>();
            rgTags.Add("tag1", "value1");
            rgTags.Add("tag3", "value3");

            // Act
            var result = TagService.GetRequiredTags(rgTags, requiredTags);

            // Assert
            Dictionary<string, string> expectedResult = new Dictionary<string, string>();
            expectedResult.Add("tag1", "value1");
            Assert.Equal<Dictionary<string, string>>(result, expectedResult);
        }

        [Fact]
        public void RgHasTwoRequiredTags()
        {
            // Arrange - Resrouce group has multiple tags and includes more than one defined "required" tag
            var requiredTags = new List<string>() { "tag1", "tag2", "tag3" };
            Dictionary<string, string> rgTags = new Dictionary<string, string>();
            rgTags.Add("tag1", "value1");
            rgTags.Add("tag2", "value2");
            rgTags.Add("tag4", "value4");

            // Act
            var result = TagService.GetRequiredTags(rgTags, requiredTags);

            // Assert
            Dictionary<string, string> expectedResult = new Dictionary<string, string>();
            expectedResult.Add("tag1", "value1");
            expectedResult.Add("tag2", "value2");
            Assert.Equal<Dictionary<string, string>>(result, expectedResult);
        }


        [Fact]
        public void ResourceHasNoTagsAppend()
        {
            // Arrange - Resrouce group has no tags. Resource Group as two required tags.
            Dictionary<string, string> resourceTags = new Dictionary<string, string>();

            Dictionary<string, string> updateTags = new Dictionary<string, string>();
            updateTags.Add("tag1", "value1");
            updateTags.Add("tag2", "value2");

            // Act
            Dictionary<string, string> result = TagService.GetTagUpdates(resourceTags, updateTags);

            // Assert - two required tags appended to resource
            Dictionary<string, string> expectedResult = new Dictionary<string, string>();
            expectedResult.Add("tag1", "value1");
            expectedResult.Add("tag2", "value2");
            Assert.Equal<Dictionary<string, string>>(result, expectedResult);
        }


        [Fact]
        public void ResourceHasTagsAppend()
        {
            // Arrange - Resrouce group one required tag. Resource Group has two required tags.
            Dictionary<string, string> resourceTags = new Dictionary<string, string>();
            resourceTags.Add("tag1", "value1");

            Dictionary<string, string> updateTags = new Dictionary<string, string>();
            updateTags.Add("tag1", "value1");
            updateTags.Add("tag2", "value2");

            // Act
            Dictionary<string, string> result = TagService.GetTagUpdates(resourceTags, updateTags);

            // Assert - second required tag appended to resource
            Dictionary<string, string> expectedResult = new Dictionary<string, string>();
            expectedResult.Add("tag1", "value1");
            expectedResult.Add("tag2", "value2");
            Assert.Equal<Dictionary<string, string>>(result, expectedResult);
        }


        [Fact]
        public void ResourceHasTagsModify()
        {
            // Arrange - Resrouce group has multiple required tags. Corresponding required tag on the 
            // resource group has a different value.
            Dictionary<string, string> resourceTags = new Dictionary<string, string>();
            resourceTags.Add("tag1", "value1");
            resourceTags.Add("tag2", "value2");

            Dictionary<string, string> updateTags = new Dictionary<string, string>();
            updateTags.Add("tag1", "value1new");
            updateTags.Add("tag2", "value2");

            // Act
            Dictionary<string, string> result = TagService.GetTagUpdates(resourceTags, updateTags);

            // Assert - tag count is same. Value of tag1 updated
            Dictionary<string, string> expectedResult = new Dictionary<string, string>();
            expectedResult.Add("tag1", "value1new");
            expectedResult.Add("tag2", "value2");
            Assert.Equal<Dictionary<string, string>>(result, expectedResult);
        }

    }
}
