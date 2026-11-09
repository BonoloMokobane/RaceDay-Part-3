
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using RaceDay.API.DTOs;
using Xunit;

namespace RaceDay.API.Tests
{
    public class RequestValidationTests
    {
        private static IList<ValidationResult> ValidateModel(object model)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(model);

            Validator.TryValidateObject(
                model,
                context,
                results,
                validateAllProperties: true);

            return results;
        }

        [Fact]
        public void EventRequest_ValidDistanceAndType_PassesValidation()
        {
            var request = new EventRequest
            {
                Name = "Community Fun Run",
                Description = "A local community event",
                EventDate = new DateTime(2027, 5, 15),
                Location = "Johannesburg",
                Distance = 5,
                EventType = "Run"
            };

            var errors = ValidateModel(request);

            Assert.Empty(errors);
        }

        [Theory]
        [InlineData("Run")]
        [InlineData("Walk")]
        [InlineData("Cycle")]
        public void EventRequest_AllowedEventTypes_PassValidation(
            string eventType)
        {
            var request = new EventRequest
            {
                Name = "Community Event",
                EventDate = new DateTime(2027, 5, 15),
                Location = "Johannesburg",
                Distance = 5,
                EventType = eventType
            };

            var errors = ValidateModel(request);

            Assert.Empty(errors);
        }

        [Theory]
        [InlineData("Swim")]
        [InlineData("Dancing")]
        public void EventRequest_DisallowedEventTypes_FailValidation(
            string eventType)
        {
            var request = new EventRequest
            {
                Name = "Community Event",
                EventDate = new DateTime(2027, 5, 15),
                Location = "Johannesburg",
                Distance = 5,
                EventType = eventType
            };

            var errors = ValidateModel(request);

            Assert.NotEmpty(errors);
        }

        [Fact]
        public void EventRequest_ZeroDistance_FailsValidation()
        {
            var request = new EventRequest
            {
                Name = "Community Event",
                EventDate = new DateTime(2027, 5, 15),
                Location = "Johannesburg",
                Distance = 0,
                EventType = "Run"
            };

            var errors = ValidateModel(request);

            Assert.NotEmpty(errors);
        }

        [Fact]
        public void RegisterRequest_InvalidRole_FailsValidation()
        {
            var request = new RegisterRequest
            {
                FirstName = "Test",
                LastName = "User",
                Email = "test@example.com",
                Password = "TestPassword123",
                Role = "Administrator"
            };

            var errors = ValidateModel(request);

            Assert.NotEmpty(errors);
        }
    }
}
