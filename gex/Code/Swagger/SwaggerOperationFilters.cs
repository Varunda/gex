using gex.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Org.BouncyCastle.Asn1.X509.Qualified;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace gex.Code.Swagger {

    public class SwaggerOperationFilters : IOperationFilter {

        private static Dictionary<Type, OpenApiSchema> _ResponseTypeCache = [];

        public SwaggerOperationFilters() {

        }

        public void Apply(OpenApiOperation op, OperationFilterContext ctx) {
            ApplyApiResponse(op, ctx);
            ApplyInternal(op, ctx);
            ApplyPermission(op, ctx);
        }

        public void ApplyApiResponse(OpenApiOperation op, OperationFilterContext ctx) {
            OpenApiResponse? res = op.Responses.GetValueOrDefault("200");
            if (res == null) {
                return;
            }

            Type retType = ctx.MethodInfo.ReturnType;

            if (retType.IsGenericType == true && retType.BaseType == typeof(Task)) {
                retType = retType.GenericTypeArguments[0];
            }

            if (retType.IsGenericType == true && retType.GetGenericTypeDefinition() == typeof(ApiResponse<>)) {
                retType = retType.GenericTypeArguments[0];
            }

            bool isList = false;
            if (retType.IsGenericType && retType.GetGenericTypeDefinition() == typeof(List<>)) {
                isList = true;

                retType = retType.GenericTypeArguments[0];
            }

            if (res.Content.ContainsKey("application/json") == false) {

                string schemaId = retType.FullName!;

                OpenApiSchema? schema = ctx.SchemaRepository.Schemas.GetValueOrDefault(schemaId);
                if (schema == null) {
                    schema = ctx.SchemaGenerator.GenerateSchema(retType, ctx.SchemaRepository);

                    if (schemaId.StartsWith("System.") == false) {
                        if (ctx.SchemaRepository.Schemas.ContainsKey(schemaId) == false) {
                            ctx.SchemaRepository.AddDefinition(schemaId, schema);
                        }
                    }
                }

                if (isList == true) {
                    res.Content.Add("application/json", new OpenApiMediaType() {
                        Schema = new OpenApiSchema() {
                            Type = "array",
                            Items = schema
                        }
                    });
                } else {
                    res.Content.Add("application/json", new OpenApiMediaType() {
                        Schema = schema
                    });
                }
            }
        }

        /// <summary>
        ///     add the internal server error response for all actions. if any response has already been defined,
        ///     the default value is added
        /// </summary>
        public void ApplyInternal(OpenApiOperation operation, OperationFilterContext context) {
            string responseStr = "An internal error was thrown. This error has been logged with a unique GUID";

            // If any additional info about this has been added, use that and the default string
            if (operation.Responses.TryGetValue("500", out OpenApiResponse? response)) {
                response.Description += "<br/><br/>" + responseStr;
            } else {
                context.SchemaRepository.TryLookupByType(typeof(ProblemDetails), out OpenApiSchema? schema);
                schema ??= context.SchemaGenerator.GenerateSchema(typeof(ProblemDetails), context.SchemaRepository);

                operation.Responses.Add("500", new OpenApiResponse { 
                    Description = responseStr,
                    Content = new Dictionary<string, OpenApiMediaType>() {
                        ["application/problem+json"] = new OpenApiMediaType() {
                            Schema = schema
                        }
                    }
                });
            }
        }

        /// <summary>
        ///     add the auth (401) and permission (403) responses. if a 403 response has already been defined,
        ///     the app permissions necessary (if <see cref="PermissionNeededAttribute"/> is set) will be added.
        /// </summary>
        public void ApplyPermission(OpenApiOperation operation, OperationFilterContext context) {
            IEnumerable<PermissionNeededAttribute> permissionGroupAttrib = 
                context.MethodInfo.DeclaringType?.GetCustomAttributes(true)
                    .Union(context.MethodInfo.GetCustomAttributes(true))
                    .OfType<PermissionNeededAttribute>() ?? new List<PermissionNeededAttribute>();

            // Add which permissions a user must have to use this action
            if (permissionGroupAttrib.Count() > 0) {
                string groupStrs = "Error parsing attribute";
                object args = permissionGroupAttrib.First().Arguments![0];
                if (args is string[] parms) {
                    groupStrs = String.Join(", ", parms);
                }

                string responseStr = "The user lacked one of the following permissions: " +  groupStrs;

                // If the response provides an explicit 403 response, include that as well
                if (!operation.Responses.ContainsKey("403")) {
                    operation.Responses.Add("403", new OpenApiResponse {
                        Description = responseStr,
                    });
                } else {
                    operation.Responses["403"].Description += "<br/><br/>" + responseStr;
                }

                // A 401 is only returned if they are not authenticated, likey an error if a 401 is set instead of 403
                if (operation.Responses.ContainsKey("401")) {
                    throw new ApplicationException($"{operation.OperationId} has defined a 401 response. Remove this");
                }

                operation.Responses.Add("401", new OpenApiResponse() { 
                    Description = "The user has not been authenticated (no session, login thru Discord), or their authentication has been challenged."
                });
            }

        }

    }
}
