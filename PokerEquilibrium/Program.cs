using System.Globalization;

using PokerEquilibrium;

// Переведем приложение на английский, для того, чтобы видеть православное NAN вместо "не число" 
Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

Console.WriteLine("Jose set up a circular poker table for his friends,\n" +
                  "so that each of the seats at the table has the same number of poker chips.\n" +
                  "But when Jose wasn't looking, someone rearranged all of the chips,\n" +
                  "so that they are no longer evenly distributed!\n" +
                  "Now Jose needs to redistribute the chips,\n" +
                  "so that every seat has the same number before his friends arrive.\n" +
                  "But Jose is very meticulous: to ensure that he doesn't lose any chips in the process,\n" +
                  "he only moves chips between adjacent seats.\n" +
                  "Moreover, he only moves chips one at a time.What is the minimum number of chip moves\n" +
                  "Jose will need to make to bring the chips back to equilibrium?");
Console.WriteLine("(Input format: 'chips: [1, 5, 9, 10, 5]')");

while (true)
{
    Console.WriteLine("Input:");

    // Читаем вводимую строку
    string? inputString = Console.ReadLine();

    // Пытаем преобразовать ее в массив интов - кол-во мест с их количеством фишек на этих местах
    if (inputString is not null && inputString.TryParse(out List<int> chipsOnPlaces))
    {
        // Создаем покерный стол
        var pokerTable = new PokerTable(chipsOnPlaces);

        // Считаем количество необходимых перемещений
        Console.WriteLine("Output:");

        // Проверяем что равновесие возможно в принципе
        if (pokerTable.CheckEquilibriumPossibility())
            Console.WriteLine(pokerTable.GetMinimumQuantityMovesToEquilibrium());
        else
            Console.WriteLine("Balance impossible with this numbers. Enter another.");
    }
}