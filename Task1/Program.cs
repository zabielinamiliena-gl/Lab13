using System;

namespace BukovynaAirport
{
    public static class EventLogger
    {
        public static void Log(string source, string message)
        {
            string time = DateTime.Now.ToString("HH:mm:ss.fff");
            Console.WriteLine($"[{time}] [{source}] {message}");
        }
    }

    public class Passenger
    {
        public string FullName { get; }
        public bool HasValidBoardingPass { get; }
        public bool HasValidPassport { get; }
        public bool IsSecurityCleared { get; }
        public double HandLuggageWeight { get; }
        public bool IsCustomsCleared { get; }

        public Passenger(string fullName, bool hasValidBoardingPass, bool hasValidPassport,
                         bool isSecurityCleared, double handLuggageWeight, bool isCustomsCleared)
        {
            FullName = fullName;
            HasValidBoardingPass = hasValidBoardingPass;
            HasValidPassport = hasValidPassport;
            IsSecurityCleared = isSecurityCleared;
            HandLuggageWeight = handLuggageWeight;
            IsCustomsCleared = isCustomsCleared;
        }
    }

    public abstract class SecurityCheckHandler
    {
        protected SecurityCheckHandler? _nextHandler;

        public SecurityCheckHandler SetNext(SecurityCheckHandler nextHandler)
        {
            _nextHandler = nextHandler;
            return nextHandler;
        }

        public void Handle(Passenger passenger)
        {
            string handlerName = GetType().Name;
            EventLogger.Log(handlerName, $"Отримано запит на перевірку пасажира: {passenger.FullName}");

            if (Check(passenger))
            {
                EventLogger.Log(handlerName, "Перевірку успішно пройдено.");
                if (_nextHandler != null)
                {
                    _nextHandler.Handle(passenger);
                }
                else
                {
                    EventLogger.Log("BoardingGate", $"Усі етапи контролю завершено! Пасажир {passenger.FullName} допущений до посадки.");
                }
            }
        }

        protected abstract bool Check(Passenger passenger);
    }

    public class BoardingPassCheck : SecurityCheckHandler
    {
        protected override bool Check(Passenger passenger)
        {
            if (!passenger.HasValidBoardingPass)
            {
                EventLogger.Log(nameof(BoardingPassCheck), "ВІДМОВА: Посадковий талон недійсний. Перевірку зупинено.");
                return false;
            }
            return true;
        }
    }

    public class PassportControlCheck : SecurityCheckHandler
    {
        protected override bool Check(Passenger passenger)
        {
            if (!passenger.HasValidPassport)
            {
                EventLogger.Log(nameof(PassportControlCheck), "ВІДМОВА: Проблеми з паспортними документами. Перевірку зупинено.");
                return false;
            }
            return true;
        }
    }

    public class SecurityScreeningCheck : SecurityCheckHandler
    {
        protected override bool Check(Passenger passenger)
        {
            if (!passenger.IsSecurityCleared)
            {
                EventLogger.Log(nameof(SecurityScreeningCheck), "ВІДМОВА: Виявлено заборонені предмети під час огляду безпеки. Перевірку зупинено.");
                return false;
            }
            return true;
        }
    }

    public class HandLuggageWeightCheck : SecurityCheckHandler
    {
        private const double MaxWeightKg = 10.0;

        protected override bool Check(Passenger passenger)
        {
            if (passenger.HandLuggageWeight > MaxWeightKg)
            {
                EventLogger.Log(nameof(HandLuggageWeightCheck),
                    $"ВІДМОВА: Перевищено допустиму вагу ручної поклажі ({passenger.HandLuggageWeight:F1} кг при нормі {MaxWeightKg:F1} кг). Перевірку зупинено.");
                return false;
            }
            return true;
        }
    }

    public class CustomsControlCheck : SecurityCheckHandler
    {
        protected override bool Check(Passenger passenger)
        {
            if (!passenger.IsCustomsCleared)
            {
                EventLogger.Log(nameof(CustomsControlCheck), "ВІДМОВА: Порушення митних правил. Перевірку зупинено.");
                return false;
            }
            return true;
        }
    }

    public class Program
    {
        private static SecurityCheckHandler BuildInternationalChain()
        {
            var boardingPass = new BoardingPassCheck();
            var passport = new PassportControlCheck();
            var customs = new CustomsControlCheck();
            var security = new SecurityScreeningCheck();
            var luggage = new HandLuggageWeightCheck();

            boardingPass.SetNext(passport)
                        .SetNext(customs)
                        .SetNext(security)
                        .SetNext(luggage);

            return boardingPass;
        }

        private static SecurityCheckHandler BuildDomesticChain()
        {
            var boardingPass = new BoardingPassCheck();
            var security = new SecurityScreeningCheck();
            var luggage = new HandLuggageWeightCheck();

            boardingPass.SetNext(security)
                        .SetNext(luggage);

            return boardingPass;
        }

        public static void DemonstratePreFlightControl()
        {
            Console.WriteLine(" СЦЕНАРІЙ 1: Міжнародний рейс (успішне проходження усіх перевірок) ===");
            SecurityCheckHandler internationalChain = BuildInternationalChain();
            var intlPassenger = new Passenger("Олена Ковальчук", true, true, true, 8.5, true);
            internationalChain.Handle(intlPassenger);

            Console.WriteLine("\n СЦЕНАРІЙ 2: Міжнародний рейс (ручна поклажа 12 кг) ===");
            var overweightPassenger = new Passenger("Іван Бойко", true, true, true, 12.0, true);
            internationalChain.Handle(overweightPassenger);

            Console.WriteLine("\n  СЦЕНАРІЙ 3: Внутрішній рейс (коротший ланцюг без паспортного контролю) ===");
            SecurityCheckHandler domesticChain = BuildDomesticChain();
            var domesticPassenger = new Passenger("Марія Ткачук", true, false, true, 7.0, false);
            domesticChain.Handle(domesticPassenger);
        }

        public static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            DemonstratePreFlightControl();
        }
    }
}