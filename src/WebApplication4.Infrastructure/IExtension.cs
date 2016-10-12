// Copyright © 2015 Dmitry Sikorsky. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace WebApplication4.Infrastructure
{
  public interface IExtension
  {
    string Name { get; }

    void ConfigureServices(IServiceCollection services);
    void RegisterRoutes(IRouteBuilder routeBuilder);
  }
}