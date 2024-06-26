namespace TransportTaskLibrary;

public static class TransportTask
{
    // Метод расчета опорного плана
    public static double[,] CalculateBasePlan(int[,] deliveryPrices, List<double> reserves, List<double> needs)
    {
        // Подготавливаем матрицу доставки
        double[,] deliveryPlan = new double[reserves.Count, needs.Count];
        for (int i = 0; i < reserves.Count; i++)
        {
            for (int j = 0; j < needs.Count; j++)
                deliveryPlan[i, j] = double.NaN;
        }

        // Работаем пока не сформируем опорный план
        while (true)
        {
            // Ищем перевозку по минимальной стоимости c максимальной нагрузкой
            // с незаблокированными осями
            int min = int.MaxValue;
            int indexOfSupplier = 0, indexOfConsumer = 0;

            for (int i = 0; i < reserves.Count; i++)
            {
                for (int j = 0; j < needs.Count; j++)
                {
                    if (!double.IsNaN(reserves[i])
                        && !double.IsNaN(needs[j]))
                    {
                        if (min > deliveryPrices[i, j])
                        {
                            min = deliveryPrices[i, j];
                            indexOfSupplier = i;
                            indexOfConsumer = j;
                        }

                        // Если минимум уже есть, но есть перевозка по той же цене, но в большем количестве
                        if (min == deliveryPrices[i, j]
                            && reserves[indexOfSupplier] - needs[indexOfConsumer] > reserves[i] - needs[j])
                        {
                            min = deliveryPrices[i, j];
                            indexOfSupplier = i;
                            indexOfConsumer = j;
                        }
                    }
                }
            }

            // Если не осталось ячеек со свободными осями выходим из цикла
            if (min == int.MaxValue)
                break;

            // Добавляем отгрузку на план и доставляем товар,
            // так же блокируем одну из изчерпанных осей
            if (reserves[indexOfSupplier] >= needs[indexOfConsumer])
            {
                deliveryPlan[indexOfSupplier, indexOfConsumer] = needs[indexOfConsumer];
                reserves[indexOfSupplier] -= needs[indexOfConsumer];
                needs[indexOfConsumer] = double.NaN;
            }
            else
            {
                deliveryPlan[indexOfSupplier, indexOfConsumer] = reserves[indexOfSupplier];
                needs[indexOfConsumer] -= reserves[indexOfSupplier];
                reserves[indexOfSupplier] = double.NaN;
            }
        }
        return deliveryPlan;
    }

    // Метод оптимизации плана доставки, методом потенциалов
    public static void OptimizePlan(int[,] deliveryPrices, double[,] deliveryPlan)
    {
        // U потенциалы
        double[] suppliersPotenial = new double[deliveryPrices.GetLength(0)];
        // V потенциалы
        double[] consumerPotenial = new double[deliveryPrices.GetLength(1)];

        // Работаем пока не будет не задействованных маршрутов с отрицательными оценками
        while (true)
        {
            // Подготавливаем массивы под итерацию цикла
            for (int i = 0; i < suppliersPotenial.Length; i++)
                suppliersPotenial[i] = double.NaN;
            for (int i = 0; i < consumerPotenial.Length; i++)
                consumerPotenial[i] = double.NaN;

            // U0 = 0 - опорный потенциал
            suppliersPotenial[0] = 0;
            // Вычисляем потенциалы
            while (suppliersPotenial.Contains(double.NaN) || consumerPotenial.Contains(double.NaN))
            {
                for (int i = 0; i < deliveryPrices.GetLength(0); i++)
                {
                    for (int j = 0; j < deliveryPrices.GetLength(1); j++)
                    {
                        if (!double.IsNaN(deliveryPlan[i, j]))
                        {
                            if (double.IsNaN(consumerPotenial[j]))
                                consumerPotenial[j] = deliveryPrices[i, j] - suppliersPotenial[i];
                            if (double.IsNaN(suppliersPotenial[i]))
                                suppliersPotenial[i] = deliveryPrices[i, j] - consumerPotenial[j];
                        }
                    }
                }
            }

            // Оценка не задействованных маршрутов
            // Цена доставки минус сумма потенциалов незадействованного маршрута
            double[,] grades = new double[deliveryPrices.GetLength(0), deliveryPrices.GetLength(1)];
            for (int i = 0; i < grades.GetLength(0); i++)
            {
                for (int j = 0; j < grades.GetLength(1); j++)
                    grades[i, j] = double.NaN;
            }

            for (int i = 0; i < deliveryPrices.GetLength(0); i++)
            {
                for (int j = 0; j < deliveryPrices.GetLength(1); j++)
                {
                    if (double.IsNaN(deliveryPlan[i, j]))
                        grades[i, j] = deliveryPrices[i, j] - suppliersPotenial[i] - consumerPotenial[j];
                }
            }

            // Ищем минимальную оценку незадействованного маршрута и его индексы
            double minGrade = double.MaxValue;
            int indexOfMinI = 0;
            int indexOfMinJ = 0;

            for (int i = 0; i < grades.GetLength(0); i++)
            {
                for (int j = 0; j < grades.GetLength(1); j++)
                {
                    if (minGrade > grades[i, j])
                    {
                        minGrade = grades[i, j];
                        indexOfMinI = i;
                        indexOfMinJ = j;
                    }
                }
            }

            // Если есть маршрут с отрицательной оценкой оптимизируем план
            if (minGrade < 0.0)
            {
                // Добавляем новый маршрут по месту маршрута с отрицательной оценкой
                deliveryPlan[indexOfMinI, indexOfMinJ] = 0.0;

                // Если получается найти маршрут оптимизации
                if (FindOptimizationRoute(deliveryPlan, indexOfMinI, indexOfMinJ, out List<RoutePoint> optimizationRoute))
                {
                    // Ищем минимальную доставку по маршруту оптимизации (Исключая созданный маршрут)
                    double minDelivery = double.MaxValue;
                    foreach (var point in optimizationRoute)
                    {
                        if (minDelivery > deliveryPlan[point.I, point.J] && (indexOfMinI != point.I || indexOfMinJ != point.J))
                            minDelivery = deliveryPlan[point.I, point.J];
                    }

                    // Оптимизируем план
                    for (int i = 0; i < optimizationRoute.Count; i++)
                    {
                        // Четные позиции увеличиваем, нечетные позиции уменьшаем
                        if (i % 2 == 0)
                        {
                            deliveryPlan[optimizationRoute[i].I, optimizationRoute[i].J] += minDelivery;
                        }
                        else
                        {
                            deliveryPlan[optimizationRoute[i].I, optimizationRoute[i].J] -= minDelivery;
                            // Если доставка стала ровна нулю убираем ее из плана
                            if (deliveryPlan[optimizationRoute[i].I, optimizationRoute[i].J] == 0.0)
                                deliveryPlan[optimizationRoute[i].I, optimizationRoute[i].J] = double.NaN;
                        }
                    }
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }
        }
    }

    // Класс точки маршрута оптимизации
    public class RoutePoint
    {
        // 0 - Вверх, 1 - Вправо, 2 - Вниз, 3 - Влево, 4 - Вверх и тд...
        // Поиск ведется по самой дальней траектории, поэтому есть ограничение поиска влево,
        // то есть поиск с отрицательным направлением блокируем
        // Содержит текущее направление поиска новой ячейки
        private int _currentDirection;
        public int CurrentDirection
        {
            get { return _currentDirection; }
            set { _currentDirection = value < 0 ? 0 : value; }
        }
        // Начальное направление поиска новой ячейки
        public int InitializationDirection { get; }
        // Координата X текущей ячейки
        public int I { get; }
        // Координата Y текущей ячейки
        public int J { get; }

        // Конструктор точки маршрута
        public RoutePoint(int i, int j, int initializationDirection)
        {
            I = i;
            J = j;
            InitializationDirection = initializationDirection;
            CurrentDirection = initializationDirection - 1;
        }
    }

    // Метод поиска маршрута оптимизации
    public static bool FindOptimizationRoute(double[,] deliveryPlan, int startPointI, int startPointJ, out List<RoutePoint> optimizationRoute)
    {
        // Создаем лист точек
        optimizationRoute =
        [
            // Добавляем стартовую точку на маршрут, ищем вверх
            new RoutePoint(startPointI, startPointJ, 0),
        ];

        // Работаем пока не составим маршрут или не убедимся, что его нет
        while (true)
        {
            // Ищем от последней точки маршрута
            RoutePoint routePoint = optimizationRoute[^1];

            // В начальной точке ищем во все стороны,
            // в последующих пока направление не поменялось на обратное
            if (optimizationRoute.Count == 1
                ? routePoint.CurrentDirection < routePoint.InitializationDirection + 4
                : routePoint.CurrentDirection < routePoint.InitializationDirection + 2)
            {
                // Заводим координаты под новую точку
                int newPointI = 0;
                int newPointJ = 0;

                // Индикатор нахождения точки
                bool pointWasFound = false;

                // Ищем в заданном направлении
                switch (routePoint.CurrentDirection % 4)
                {
                    case 0:
                        pointWasFound = TryFindRoutePointUp(deliveryPlan, routePoint.I, routePoint.J, out newPointI, out newPointJ);
                        break;
                    case 1:
                        pointWasFound = TryFindRoutePointRight(deliveryPlan, routePoint.I, routePoint.J, out newPointI, out newPointJ);
                        break;
                    case 2:
                        pointWasFound = TryFindRoutePointDown(deliveryPlan, routePoint.I, routePoint.J, out newPointI, out newPointJ);
                        break;
                    case 3:
                        pointWasFound = TryFindRoutePointLeft(deliveryPlan, routePoint.I, routePoint.J, out newPointI, out newPointJ);
                        break;
                }

                // Если точка была найдена
                if (pointWasFound)
                {
                    // И еще не вернулись в исходную точку, добавляем точку
                    if (newPointI != startPointI || newPointJ != startPointJ)
                    {
                        optimizationRoute.Add(new RoutePoint(newPointI, newPointJ, routePoint.CurrentDirection));
                    }
                    // Иначе выходим из цикла, так как вернулись в исходную точку
                    else
                    {
                        // Удаляем промежуточные точки (Нужны только вершины многоугольника)
                        if (optimizationRoute.Count >= 4)
                        {
                            for (int i = 0; i < optimizationRoute.Count - 1; i++)
                            {
                                if (optimizationRoute[i].CurrentDirection == optimizationRoute[i + 1].CurrentDirection)
                                {
                                    optimizationRoute.RemoveAt(i);
                                    i--;
                                }
                            }
                        }

                        return true;
                    }
                }
                // Иначе меняем направление поиска
                else
                {
                    routePoint.CurrentDirection++;
                }
            }
            else
            {
                // Если точка тупиковая, удаляем ее
                optimizationRoute.Remove(routePoint);

                // Выкидываем из цикла, если маршрут схлопнулся, в случае невозможности построения
                if (optimizationRoute.Count == 0)
                    return false;

                // При возвращении в предыдущую точку меняем направление
                routePoint = optimizationRoute[^1];
                routePoint.CurrentDirection++;
            }
        }
    }

    // Метод поиска точки вверх
    private static bool TryFindRoutePointUp(double[,] deliveryPlan, int startPointI, int startPointJ, out int newPointI, out int newPointJ)
    {
        newPointI = 0;
        newPointJ = 0;

        for (int i = startPointI; i >= 0; i--)
        {
            if (!double.IsNaN(deliveryPlan[i, startPointJ]) && i != startPointI)
            {
                newPointI = i;
                newPointJ = startPointJ;
                return true;
            }
        }

        return false;
    }

    // Метод поиска точки вниз
    private static bool TryFindRoutePointDown(double[,] deliveryPlan, int startPointI, int startPointJ, out int newPointI, out int newPointJ)
    {
        newPointI = 0;
        newPointJ = 0;

        for (int i = startPointI; i < deliveryPlan.GetLength(0); i++)
        {
            if (!double.IsNaN(deliveryPlan[i, startPointJ]) && i != startPointI)
            {
                newPointI = i;
                newPointJ = startPointJ;
                return true;
            }
        }

        return false;
    }

    // Метод поиска точки вправо
    private static bool TryFindRoutePointRight(double[,] deliveryPlan, int startPointI, int startPointJ, out int newPointI, out int newPointJ)
    {
        newPointI = 0;
        newPointJ = 0;

        for (int j = startPointJ; j < deliveryPlan.GetLength(1); j++)
        {
            if (!double.IsNaN(deliveryPlan[startPointI, j]) && j != startPointJ)
            {
                newPointI = startPointI;
                newPointJ = j;
                return true;
            }
        }

        return false;
    }

    // Метод поиска точки влево
    private static bool TryFindRoutePointLeft(double[,] deliveryPlan, int startPointI, int startPointJ, out int newPointI, out int newPointJ)
    {
        newPointI = 0;
        newPointJ = 0;

        for (int j = startPointJ; j >= 0; j--)
        {
            if (!double.IsNaN(deliveryPlan[startPointI, j]) && j != startPointJ)
            {
                newPointI = startPointI;
                newPointJ = j;
                return true;
            }
        }

        return false;
    }

    // Метод вычисления стоимости доставки
    public static int CalculatePriceOfDelivery(int[,] deliveryPrices, double[,] deliveryPlan)
    {
        int priceOfTransport = 0;

        for (int i = 0; i < deliveryPrices.GetLength(0); i++)
        {
            for (int j = 0; j < deliveryPrices.GetLength(1); j++)
            {
                if (!double.IsNaN(deliveryPlan[i, j]))
                    priceOfTransport += deliveryPrices[i, j] * (int)deliveryPlan[i, j];
            }
        }

        return priceOfTransport;
    }
}