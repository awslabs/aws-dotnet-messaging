global using System;
global using System.Collections.Generic;
global using System.Collections.Concurrent;
global using System.Linq;
global using System.IO;
global using System.Text;
global using System.Threading;
global using System.Threading.Tasks;

global using Microsoft.Extensions.DependencyInjection;

global using AWS.MessageProcessing.Configuration;
global using AWS.MessageProcessing.MessagePump;
global using AWS.MessageProcessing.Serialization;

global using Amazon.SQS;
global using Amazon.SQS.Model;

global using Moq;
global using Xunit;

global using static AWS.MessageProcessing.UnitTests.TestUtilties;