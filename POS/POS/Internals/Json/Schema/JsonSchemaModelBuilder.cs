﻿#region License
// Copyright (c) 2007 James Newton-King
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lib.JSON.Schema
{
    internal class JsonSchemaModelBuilder
    {
        private JsonSchemaNodeCollection _nodes = new JsonSchemaNodeCollection();
        private Dictionary<JsonSchemaNode, JsonSchemaModel> _nodeModels = new Dictionary<JsonSchemaNode, JsonSchemaModel>();
        private JsonSchemaNode _node ;
        
        public JsonSchemaModel Build(JsonSchema schema)
        {
            this._nodes = new JsonSchemaNodeCollection();
            this._node = this.AddSchema(null, schema);

            this._nodeModels = new Dictionary<JsonSchemaNode, JsonSchemaModel>();
            JsonSchemaModel model = this.BuildNodeModel(_node);

            return model;
        }
        
        public JsonSchemaNode AddSchema(JsonSchemaNode existingNode, JsonSchema schema)
        {
            string newId;
            if (existingNode != null)
            {
                if (existingNode.Schemas.Contains(schema))
                {
                    return existingNode;
                }
                
                newId = JsonSchemaNode.GetId(existingNode.Schemas.Union(new[] { schema }));
            }
            else
            {
                newId = JsonSchemaNode.GetId(new[] { schema });
            }
            
            if (this._nodes.Contains(newId))
            {
                return this._nodes[newId];
            }
            
            JsonSchemaNode currentNode = (existingNode != null)
                                         ? existingNode.Combine(schema)
                                         : new JsonSchemaNode(schema);
            
            this._nodes.Add(currentNode);
            
            this.AddProperties(schema.Properties, currentNode.Properties);
            
            this.AddProperties(schema.PatternProperties, currentNode.PatternProperties);
            
            if (schema.Items != null)
            {
                for (int i = 0; i < schema.Items.Count; i++)
                {
                    this.AddItem(currentNode, i, schema.Items[i]);
                }
            }
            
            if (schema.AdditionalProperties != null)
            {
                this.AddAdditionalProperties(currentNode, schema.AdditionalProperties);
            }
            
            if (schema.Extends != null)
            {
                currentNode = this.AddSchema(currentNode, schema.Extends);
            }

            return currentNode;
        }
        
        public void AddProperties(IDictionary<string, JsonSchema> source, IDictionary<string, JsonSchemaNode> target)
        {
            if (source != null)
            {
                foreach (KeyValuePair<string, JsonSchema> property in source)
                {
                    this.AddProperty(target, property.Key, property.Value);
                }
            }
        }
        
        public void AddProperty(IDictionary<string, JsonSchemaNode> target, string propertyName, JsonSchema schema)
        {
            JsonSchemaNode propertyNode;
            target.TryGetValue(propertyName, out propertyNode);

            target[propertyName] = this.AddSchema(propertyNode, schema);
        }
        
        public void AddItem(JsonSchemaNode parentNode, int index, JsonSchema schema)
        {
            JsonSchemaNode existingItemNode = (parentNode.Items.Count > index)
                                              ? parentNode.Items[index]
                                              : null;
            
            JsonSchemaNode newItemNode = this.AddSchema(existingItemNode, schema);
            
            if (!(parentNode.Items.Count > index))
            {
                parentNode.Items.Add(newItemNode);
            }
            else
            {
                parentNode.Items[index] = newItemNode;
            }
        }
        
        public void AddAdditionalProperties(JsonSchemaNode parentNode, JsonSchema schema)
        {
            parentNode.AdditionalProperties = this.AddSchema(parentNode.AdditionalProperties, schema);
        }
        
        private JsonSchemaModel BuildNodeModel(JsonSchemaNode node)
        {
            JsonSchemaModel model;
            if (this._nodeModels.TryGetValue(node, out model))
            {
                return model;
            }
      
            model = JsonSchemaModel.Create(node.Schemas);
            this._nodeModels[node] = model;
            
            foreach (KeyValuePair<string, JsonSchemaNode> property in node.Properties)
            {
                if (model.Properties == null)
                {
                    model.Properties = new Dictionary<string, JsonSchemaModel>();
                }
                
                model.Properties[property.Key] = this.BuildNodeModel(property.Value);
            }
            foreach (KeyValuePair<string, JsonSchemaNode> property in node.PatternProperties)
            {
                if (model.PatternProperties == null)
                {
                    model.PatternProperties = new Dictionary<string, JsonSchemaModel>();
                }
                
                model.PatternProperties[property.Key] = this.BuildNodeModel(property.Value);
            }
            foreach (JsonSchemaNode t in node.Items)
            {
                if (model.Items == null)
                {
                    model.Items = new List<JsonSchemaModel>();
                }
                
                model.Items.Add(this.BuildNodeModel(t));
            }
            if (node.AdditionalProperties != null)
            {
                model.AdditionalProperties = this.BuildNodeModel(node.AdditionalProperties);
            }
            
            return model;
        }
    }
}