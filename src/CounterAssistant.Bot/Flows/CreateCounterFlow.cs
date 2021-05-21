using CounterAssistant.Domain.Builders;
using CounterAssistant.Domain.Models;
using System;

namespace CounterAssistant.Bot.Flows
{
    public class CreateCounterFlow 
    {
        private CreateFlowSteps _currentStep;
        private CounterBuilder _builder;

        public CreateCounterFlow()
        {
            _currentStep = CreateFlowSteps.None;
            _builder = CounterBuilder.Default;
        }

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
