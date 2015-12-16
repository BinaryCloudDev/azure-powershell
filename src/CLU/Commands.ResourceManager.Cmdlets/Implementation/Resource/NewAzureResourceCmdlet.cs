﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

namespace Microsoft.Azure.Commands.ResourceManager.Cmdlets.Implementation
{
    using Microsoft.Azure.Commands.Common;
    using Microsoft.Azure.Commands.ResourceManager.Cmdlets.Components;
    using Microsoft.Azure.Commands.ResourceManager.Cmdlets.Entities.Resources;
    using Microsoft.Azure.Commands.ResourceManager.Cmdlets.Extensions;
    using Newtonsoft.Json.Linq;
    using System.Collections;
    using System.Linq;
    using System.Management.Automation;

    /// <summary>
    /// A cmdlet that creates a new azure resource.
    /// </summary>
    [Cmdlet(VerbsCommon.New, 
            "AzureRmResource", 
            SupportsShouldProcess = true, 
            DefaultParameterSetName = ResourceManipulationCmdletBase.ResourceIdParameterSet), 
    OutputType(typeof(Resource<JToken>))]
    public sealed class NewAzureResourceCmdlet : ResourceManipulationCmdletBase
    {
        /// <summary>
        /// Gets or sets the location.
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The resource location.")]
        [ValidateNotNullOrEmpty]
        [Alias("l")]
        public string Location { get; set; }

        /// <summary>
        /// Gets or sets the kind.
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "The resource kind.")]
        [ValidateNotNullOrEmpty]
        [Alias("k")]
        public string Kind { get; set; }

        /// <summary>
        /// Gets or sets the property object.
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "A hash table which represents resource properties.")]
        [ValidateNotNullOrEmpty]
        [Alias("PropertyObject")]
        public PSObject Properties { get; set; }

        /// <summary>
        /// Gets or sets the plan object.
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "A hash table which represents resource plan properties.")]
        [ValidateNotNullOrEmpty]
        [Alias("plan")]
        public Hashtable PlanObject { get; set; }

        /// <summary>  
        /// Gets or sets the plan object.  
        /// </summary>  
        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true, HelpMessage = "A hash table which represents sku properties.")]  
        [ValidateNotNullOrEmpty]
        [Alias("sku")]
        public Hashtable SkuObject { get; set; }  


        /// <summary>
        /// Gets or sets the tags.
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "A hash table which represents resource tags.")]
        [Alias("Tags", "t")]
        public Hashtable[] Tag { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates if the full object was passed it.
        /// </summary>
        [Parameter(Mandatory = false, HelpMessage = "When set indicates that the full object is passed in to the -PropertyObject parameter.")]
        public SwitchParameter IsFullObject { get; set; }

        /// <summary>
        /// Executes the cmdlet.
        /// </summary>
        protected override void OnProcessRecord()
        {
            base.OnProcessRecord();

            var resourceId = this.GetResourceId();
            this.ConfirmAction(
                this.Force,
                "Are you sure you want to create the following resource: "+ resourceId,
                "Creating the resource...",
                resourceId,
                () =>
                {
                    var apiVersion = this.DetermineApiVersion(resourceId: resourceId).Result;
                    var resourceBody = this.GetResourceBody();

                    var operationResult = this.GetResourcesClient()
                        .PutResource(
                            resourceId: resourceId,
                            apiVersion: apiVersion,
                            resource: resourceBody,
                            cancellationToken: this.CancellationToken.Value,
                            odataQuery: this.ODataQuery)
                        .Result;

                    var managementUri = this.GetResourcesClient()
                      .GetResourceManagementRequestUri(
                          resourceId: resourceId,
                          apiVersion: apiVersion,
                          odataQuery: this.ODataQuery);

                    var activity = string.Format("PUT {0}", managementUri.PathAndQuery);
                    var result = this.GetLongRunningOperationTracker(activityName: activity, isResourceCreateOrUpdate: true)
                        .WaitOnOperation(operationResult: operationResult);

                    this.TryConvertToResourceAndWriteObject(result);
                });
        }

        /// <summary>
        /// Gets the resource body from the parameters.
        /// </summary>
        private JToken GetResourceBody()
        {
            return this.IsFullObject
                ? this.Properties.ToResourcePropertiesBody().ToJToken()
                : this.GetResource().ToJToken();
        }

        /// <summary>
        /// Constructs the resource assuming that only the properties were passed in.
        /// </summary>
        private Resource<JToken> GetResource()
        {
            return new Resource<JToken>()
            {
                Location = this.Location,
                Kind = this.Kind,
                Plan = this.PlanObject.ToDictionary(addValueLayer: false).ToJson().FromJson<ResourcePlan>(),
                Sku = this.SkuObject.ToDictionary(addValueLayer: false).ToJson().FromJson<ResourceSku>(),
                Tags = TagsHelper.GetTagsDictionary(this.Tag),
                Properties = this.Properties.ToResourcePropertiesBody(),
            };
        }


    }
}