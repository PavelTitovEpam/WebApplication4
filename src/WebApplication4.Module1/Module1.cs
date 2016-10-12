// Copyright © 2015 Dmitry Sikorsky. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using WebApplication4.Infrastructure;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace WebApplication4.Modules
{
  public class Module1 : IExtension
  {
    public string Name
    {
      get
      {
        return "Module1";
      }
    }

    public void ConfigureServices(IServiceCollection services)
    {
    }

    public void RegisterRoutes(IRouteBuilder routeBuilder)
    {
      routeBuilder.MapRoute(name: "module1", template: "module1", defaults: new { controller = "Module1", action = "Index" });
    }
  }
}