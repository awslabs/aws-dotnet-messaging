// Copyright Amazon.com, Inc. or its affiliates. All Rights Reserved.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AWS.Messaging.Serialization
{
    internal class DictionaryStringObjectConverter : JsonConverter<Dictionary<string, object>>
    {
        public override Dictionary<string, object>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            var value = new Dictionary<string, object>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return value;
                }

                var keyString = reader.GetString();

                if (keyString == null)
                {
                    throw new JsonException($"Unable to convert \"{keyString}\" to System.Int32.");
                }

                reader.Read();

                var itemValue = reader.GetString();
                if (itemValue == null)
                {
                    throw new JsonException($"Unable to convert \"{keyString}\" to System.Int32.");
                }

                value.Add(keyString, itemValue);
            }

            throw new JsonException("Error Occured");
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<string, object> value, JsonSerializerOptions options) => throw new NotImplementedException();
    }
}
