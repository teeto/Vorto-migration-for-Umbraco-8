using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Web.HealthCheck;
using Our.Umbraco.Vorto.Models;

namespace iCreativos.Utils.HealthChecks
{
    [HealthCheck("1DD9A117-85CB-4DDA-8385-9AF41BC3EA81",
        "Vorto Health Check",
        Description = "Checks the status of vorto fields",
        Group = "Vorto")]
    public class VortoHealthCheck : HealthCheck
    {
        private readonly IContentService _contentService;

        public VortoHealthCheck(IContentService contentService) : base() {
            this._contentService = contentService;
        }

        
        public override IEnumerable<HealthCheckStatus> GetStatus()
        {
            var info = new InfoVM();
            

            FixAllContent(ref info, fix: false);

            var descripcion = string.Format("Found {0} of {1} vorto properties in {2} content items. {3} errors.",
                info.vortoPropiedades, info.total, info.totalContent, info.errores.Count);
            foreach (var item in info.errores)
            {
                descripcion += "<br/>" + item;
            }

            return new HealthCheckStatus("Vorto Properties")
            {
                ResultType = StatusResultType.Info,
                Description = descripcion,
                Actions = new HealthCheckAction("Fix", this.Id)
                {
                    Name = "Fix"
                }.AsEnumerableOfOne<HealthCheckAction>()
            }.AsEnumerableOfOne<HealthCheckStatus>();
        }



        // fix
        public override HealthCheckStatus ExecuteAction(HealthCheckAction action)
        {


            var info = new InfoVM();
            FixAllContent(ref info, fix: true);

            var descripcion = string.Format("Fixed {0} of {1} vorto properties in {2} content items. {3} errors.",
                info.vortoPropiedades, info.total, info.totalContent, info.errores.Count);
            foreach (var item in info.errores)
            {
                descripcion += "<br/>" + item;
            }

            return new HealthCheckStatus("Done")
            {
                Description = descripcion
            };
        }
        public void FixAllContent(ref InfoVM info, bool fix = true)
        {
            var rootContent = _contentService.GetRootContent().ToList();

            //pruebas
            //var rootContent = new List<IContent>();
            //rootContent.Add(_contentService.GetById(1379));

            foreach (var item in rootContent)
            {
                FixContent(item, ref info, fix: fix);
            }
        }

        public void FixContent(IContent content, ref InfoVM info, bool fix = true)
        {
            

            var properties = content
                .Properties
                //.Where(x => x.PropertyType.PropertyEditorAlias == "Umbraco.Label")
                .ToList();

            if (properties.Any())
                info.totalContent++;

            var tipoContenido = content.ContentType.Alias;

            foreach (var property in properties)
            {
                var propertyAlias = property.Alias;
                info.total++;
                var stringValue = property.GetValue().TryConvertTo<string>();
                if(string.IsNullOrWhiteSpace(stringValue.Result) && property.Values.Any())
                {
                    //stringValue = property.Values.First().PublishedValue.TryConvertTo<string>();
                    stringValue = property.Values.First().EditedValue.TryConvertTo<string>();
                }

                if (stringValue.Success)
                {
                    if (!string.IsNullOrWhiteSpace(stringValue.Result))
                    {
                        if (stringValue.Result.DetectIsJson() && stringValue.Result.ToLower().Contains("dtdGuid".ToLower()))
                        {
                            var valor = JsonConvert.DeserializeObject<VortoValue>(stringValue.Result);
                            if (valor == null)
                            {
                                var mensajeError = "Its not possible to convert to VortoValue this property: " + propertyAlias + " of " + tipoContenido;
                                if(!info.errores.Contains(mensajeError))
                                    info.errores.Add(mensajeError);
                            }
                            else
                            {
                                info.vortoPropiedades++;
                                if (!content.ContentType.VariesByCulture())
                                {
                                    var mensajeError = "This content type " + tipoContenido + " is not allowed to change variant by culture and it has Vorto properties.";
                                    if (!info.errores.Contains(mensajeError))
                                        info.errores.Add(mensajeError);
                                }
                                if (fix)
                                {
                                    
                                        foreach (var valorIdioma in valor.Values)
                                        {
                                            try
                                            {
                                                content.SetValue(propertyAlias, valorIdioma.Value, culture: valorIdioma.Key);
                                            }
                                            catch (Exception ex)
                                            {
                                                var ex2 = new Exception("Error saving content " + content.Id + ", property: " + propertyAlias + ". " + ex.Message, ex);
                                                throw ex2;
                                            }
                                        }
                                    
                                }
                            }
                        }
                    }
                    
                }
            }

            if (fix && content.IsDirty())
            {
                if (content.Published)
                    _contentService.SaveAndPublish(content);
                else
                    _contentService.Save(content);

            }

            long totalChildren;
            IEnumerable<IContent> children = _contentService.GetPagedChildren(content.Id, 0, 10000, out totalChildren);
            foreach (var child in children)
            {
                FixContent(child, ref info, fix: fix);
            }


        }
        
        public class InfoVM
        {
            public int vortoPropiedades { get; set; }
            public int total { get; set; }
            public int totalContent { get; set; }

            public List<string> errores { get; set; }

            public InfoVM()
            {
                this.errores = new List<string>();
            }
        }
    }
}
