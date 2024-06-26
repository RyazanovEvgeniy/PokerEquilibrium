using TransportTaskLibrary;

namespace PokerEquilibrium;

public class PokerTable
{
    // Поле содержащее количество фишек на местах
    private readonly List<int> chipsOnPlaces;

    // Конструктор стола
    public PokerTable(List<int> chipsOnPlaces) => this.chipsOnPlaces = chipsOnPlaces;

    // Метод проверки возможности равновесия фишек на покерном столе
    public bool CheckEquilibriumPossibility() => chipsOnPlaces.Sum() % chipsOnPlaces.Count == 0;

    // Метод расчета минимального количества перемещений для равновесия
    public int GetMinimumQuantityMovesToEquilibrium()
    {
        if (!CheckEquilibriumPossibility())
        {
            Console.WriteLine("Balance impossible with this numbers.");
            return 0;
        }

        // **********************************************************
        // ******* Приводим нашу задачу к транспортной задаче *******
        // **********************************************************

        // Считаем точку равновесия
        int equilibrium = chipsOnPlaces.Sum() / chipsOnPlaces.Count;
        // Количество мест, в дальнейшем для расчета
        int quantityPlaces = chipsOnPlaces.Count;

        // Заводим листы поставщиков и потребителей
        // Первый индекс места за столом, второе кол-во фишек необходимое или излишнее
        List<Tuple<int, double>> suppliers = new List<Tuple<int, double>>();
        List<Tuple<int, double>> consumers = new List<Tuple<int, double>>();

        // Заполняем листы поставщиков и потребителей
        for (int i = 0; i < chipsOnPlaces.Count; i++)
        {
            if (chipsOnPlaces[i] > equilibrium)
                suppliers.Add(new Tuple<int, double>(i, chipsOnPlaces[i] - equilibrium));
            if (chipsOnPlaces[i] < equilibrium)
                consumers.Add(new Tuple<int, double>(i, equilibrium - chipsOnPlaces[i]));
        }

        // Строим матрицу цен транспортировки для транспортной задачи
        int[,] deliveryPrices = new int[suppliers.Count, consumers.Count];

        for (int i = 0; i < suppliers.Count; i++)
        {
            for (int j = 0; j < consumers.Count; j++)
            {
                deliveryPrices[i, j] = Math.Min(
                    Math.Abs(suppliers[i].Item1 - consumers[j].Item1),
                    quantityPlaces - Math.Abs(suppliers[i].Item1 - consumers[j].Item1));
            }
        }

        // Создаем два листа запасов и потребностей для транспортной задачи
        List<double> reserves = new List<double>();
        foreach (var supplier in suppliers)
            reserves.Add(supplier.Item2);
        List<double> needs = new List<double>();
        foreach (var consumer in consumers)
            needs.Add(consumer.Item2);

        // Расcчитываем опорный план
        double[,] deliveryPlan = TransportTask.CalculateBasePlan(deliveryPrices, reserves, needs);

        // Оптимизируем план
        TransportTask.OptimizePlan(deliveryPrices, deliveryPlan);

        // Смотрим стоимость доставки плану
        return TransportTask.CalculatePriceOfDelivery(deliveryPrices, deliveryPlan);
    }
}