using System;
using System.Collections.Generic;

namespace BukovynaAirport
{
    class Journal
    {
        private int minutes;

        public Journal(int startHour, int startMinute)
        {
            minutes = startHour * 60 + startMinute;
        }

        public void Write(string source, string message)
        {
            int hour = minutes / 60;
            int minute = minutes % 60;
            Console.WriteLine("[" + hour.ToString("00") + ":" + minute.ToString("00") + "] [" + source + "] " + message);
            minutes += 1;
        }
    }

    class Flight
    {
        public string Number { get; }
        public string Time { get; private set; }
        public string Gate { get; private set; }
        public string Status { get; private set; }

        public Flight(string number, string time, string gate)
        {
            Number = number;
            Time = time;
            Gate = gate;
            Status = "за розкладом";
        }

        public void SetTime(string time)
        {
            Time = time;
        }

        public void SetGate(string gate)
        {
            Gate = gate;
        }

        public void Cancel()
        {
            Status = "скасовано";
        }

        public void Restore()
        {
            Status = "за розкладом";
        }

        public string Text()
        {
            return Number + " | " + Time + " | вихід " + Gate + " | " + Status;
        }
    }

    class Schedule
    {
        private readonly List<Flight> flights = new List<Flight>();

        public void Add(Flight flight)
        {
            flights.Add(flight);
        }

        public void Print(Journal journal)
        {
            foreach (Flight flight in flights)
            {
                journal.Write("Розклад", flight.Text());
            }
        }
    }

    interface ICommand
    {
        string Name { get; }
        void Execute(Journal journal);
        void Undo(Journal journal);
    }

    class RescheduleCommand : ICommand
    {
        private readonly Flight flight;
        private readonly string newTime;
        private string oldTime;

        public RescheduleCommand(Flight flight, string newTime)
        {
            this.flight = flight;
            this.newTime = newTime;
        }

        public string Name
        {
            get { return "Перенесення рейсу " + flight.Number + " на " + newTime; }
        }

        public void Execute(Journal journal)
        {
            oldTime = flight.Time;
            flight.SetTime(newTime);
            journal.Write("Перенесення " + flight.Number, "Виконано: час " + oldTime + " змінено на " + newTime);
        }

        public void Undo(Journal journal)
        {
            flight.SetTime(oldTime);
            journal.Write("Перенесення " + flight.Number, "Скасовано: час повернуто на " + oldTime);
        }
    }

    class ChangeGateCommand : ICommand
    {
        private readonly Flight flight;
        private readonly string newGate;
        private string oldGate;

        public ChangeGateCommand(Flight flight, string newGate)
        {
            this.flight = flight;
            this.newGate = newGate;
        }

        public string Name
        {
            get { return "Зміна виходу рейсу " + flight.Number + " на " + newGate; }
        }

        public void Execute(Journal journal)
        {
            oldGate = flight.Gate;
            flight.SetGate(newGate);
            journal.Write("Зміна виходу " + flight.Number, "Виконано: вихід " + oldGate + " змінено на " + newGate);
        }

        public void Undo(Journal journal)
        {
            flight.SetGate(oldGate);
            journal.Write("Зміна виходу " + flight.Number, "Скасовано: вихід повернуто на " + oldGate);
        }
    }

    class CancelFlightCommand : ICommand
    {
        private readonly Flight flight;
        private string oldStatus;

        public CancelFlightCommand(Flight flight)
        {
            this.flight = flight;
        }

        public string Name
        {
            get { return "Скасування рейсу " + flight.Number; }
        }

        public void Execute(Journal journal)
        {
            oldStatus = flight.Status;
            flight.Cancel();
            journal.Write("Скасування " + flight.Number, "Виконано: статус «" + oldStatus + "» змінено на «скасовано»");
        }

        public void Undo(Journal journal)
        {
            flight.Restore();
            journal.Write("Скасування " + flight.Number, "Скасовано: статус повернуто на «" + oldStatus + "»");
        }
    }

    class Dispatcher
    {
        private readonly Journal journal;
        private readonly Stack<ICommand> done = new Stack<ICommand>();
        private readonly Queue<ICommand> queue = new Queue<ICommand>();
        private readonly List<string> actionLog = new List<string>();

        public Dispatcher(Journal journal)
        {
            this.journal = journal;
        }

        public void Run(ICommand command)
        {
            journal.Write("Диспетчер", "Отримано запит: " + command.Name);
            command.Execute(journal);
            done.Push(command);
            actionLog.Add("Виконано: " + command.Name);
        }

        public void Plan(ICommand command)
        {
            queue.Enqueue(command);
            journal.Write("Диспетчер", "Підготовлено в чергу: " + command.Name);
        }

        public void RunQueue()
        {
            journal.Write("Диспетчер", "Пакет передано на виконання, дій у черзі: " + queue.Count);
            while (queue.Count > 0)
            {
                Run(queue.Dequeue());
            }
        }

        public void Undo()
        {
            journal.Write("Диспетчер", "Отримано запит скасувати останню дію");
            ICommand command = done.Pop();
            command.Undo(journal);
            actionLog.Add("Скасовано: " + command.Name);
        }

        public void PrintActionLog()
        {
            journal.Write("Журнал дій", "Усі дії диспетчера");
            foreach (string line in actionLog)
            {
                journal.Write("Журнал дій", line);
            }
        }
    }

    class Program
    {
        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Situation2();
        }

        static void Situation2()
        {
            Journal journal = new Journal(9, 0);
            Schedule schedule = new Schedule();
            Flight ps101 = new Flight("PS-101", "10:30", "A2");
            Flight ps202 = new Flight("PS-202", "12:15", "B4");
            Flight ps303 = new Flight("PS-303", "14:00", "C1");
            schedule.Add(ps101);
            schedule.Add(ps202);
            schedule.Add(ps303);
            Dispatcher dispatcher = new Dispatcher(journal);

            journal.Write("Сценарій", "Початковий розклад");
            schedule.Print(journal);

            journal.Write("Сценарій", "Перенести PS-101 на 15:30 і змінити вихід PS-202");
            dispatcher.Run(new RescheduleCommand(ps101, "15:30"));
            dispatcher.Run(new ChangeGateCommand(ps202, "C3"));
            schedule.Print(journal);

            journal.Write("Сценарій", "Скасувати дві останні дії");
            dispatcher.Undo();
            dispatcher.Undo();
            schedule.Print(journal);

            journal.Write("Сценарій", "Три зміни в черзі і виконання пакетом");
            dispatcher.Plan(new RescheduleCommand(ps303, "16:40"));
            dispatcher.Plan(new ChangeGateCommand(ps101, "D1"));
            dispatcher.Plan(new CancelFlightCommand(ps202));
            dispatcher.RunQueue();
            schedule.Print(journal);
            dispatcher.PrintActionLog();
        }
    }
}
