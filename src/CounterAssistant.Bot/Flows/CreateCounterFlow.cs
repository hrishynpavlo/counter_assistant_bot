using CounterAssistant.Domain.Builders;
using CounterAssistant.Domain.Models;
using System;
using System.Collections.Generic;

namespace CounterAssistant.Bot.Flows
{
    public class CreateCounterFlow 
    {
        private CreateFlowSteps _currentStep;
        private CounterBuilder _builder;

        public CreateFlowSteps State => _currentStep;
        public Dictionary<string, object> Args => _builder.GetArgs();

        private CreateCounterFlow(CreateFlowSteps step, CounterBuilder builder)
        {
            _currentStep = step;
            _builder = builder;
        }

        public CreateCounterFlow() : this(CreateFlowSteps.None, CounterBuilder.Default) { }

        public CreateCounterResult Perform(string message)
        {
            switch (_currentStep)
            {
                case CreateFlowSteps.None:
                    {
                        _currentStep = CreateFlowSteps.SetCounterName;
                        return new CreateCounterResult { IsSuccess = false, Message = "Введите название счётчика:" };
                    }
                case CreateFlowSteps.SetCounterName:
                    {
                        _builder.WithName(message);
                        _currentStep = CreateFlowSteps.SetCounterStep;
                        return new CreateCounterResult { IsSuccess = false, Message = "Введите шаг счётчика:" };
                    }
                case CreateFlowSteps.SetCounterStep:
                    {
                        var step = ushort.Parse(message);
                        _builder.WithStep(step);
                        _currentStep = CreateFlowSteps.Completed;
                        var counter = _builder.Build();
                        return new CreateCounterResult { IsSuccess = true, Counter = counter, Message = $"Счётчик <b>{counter.Title.ToUpper()}</b> успешно создан" };
                    }
                default: throw new Exception();
            }
        }

        public static CreateCounterFlow RestoreFromContext(User user)
        {
            if (user == null || user.BotInfo == null) throw new ArgumentNullException(nameof(user));

            if(user.BotInfo.CreateCounterFlowInfo == null)
            {
                return new CreateCounterFlow();
            }

            var builder = CounterBuilder.Default;

            if (string.IsNullOrWhiteSpace(user.BotInfo.CreateCounterFlowInfo.State) || !Enum.TryParse<CreateFlowSteps>(user.BotInfo.CreateCounterFlowInfo.State, ignoreCase: true, out var step)) 
            {
                step = CreateFlowSteps.None;
            }

            if(user.BotInfo.CreateCounterFlowInfo.Args != null)
            {
                if (user.BotInfo.CreateCounterFlowInfo.Args.TryGetValue("step", out var counterStep))
                {
                    builder.WithStep((ushort)counterStep);
                }

                if (user.BotInfo.CreateCounterFlowInfo.Args.TryGetValue("name", out var counterName))
                {
                    builder.WithName((string)counterName);
                }
            }

            return new CreateCounterFlow(step, builder);
        }
    }

    public class CreateCounterResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public Counter Counter { get; set; }
    }

    public enum CreateFlowSteps
    {
        None,
        SetCounterName,
        SetCounterStep,
        Completed
    }
}
