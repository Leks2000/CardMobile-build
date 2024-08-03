using UnityEngine;

namespace Assets.Utility
{
    /// <summary>
    /// Класс для вывода ошибок для компонентов
    /// </summary>
    public static class DebugUtility
    {
        /// <summary>
        /// Выводит ошибку, если компонент в данном контроллере не найден
        /// </summary>
        /// <typeparam name="TO">То что мы ищем</typeparam>
        /// <typeparam name="TS">Где оно должно находиться</typeparam>
        /// <param name="component">Результат нашего поиска</param>
        /// <param name="source">Где оно должно находиться</param>
        /// <param name="onObject">Сам объект, где мы это делаем</param>
        /// <example>
        ///   m_Health = GetComponent<Health>();
        ///   DebugUtility.HandleErrorIfNullGetComponent<Health, EnemyController>(m_Health, this, gameObject);
        /// </example>
        public static void HandleErrorIfNullGetComponent<TO, TS>(
            Component component,
            Component source,
            GameObject onObject)
        {
            if (component == null)
            {
                Debug.LogError($"Ошибка: Компонент типа {typeof(TS)} on GameObject {source.gameObject.name}" +
                               $" ожидалось, что вы найдете компонент типа {typeof(TO)} on GameObject {onObject.name}" +
                               ", но ни один из них не был найден..");
            }
        }

        /// <summary>
        /// Выводит ошибку, если объект в данном контроллере не найден
        /// </summary>
        /// <typeparam name="TO">То что мы ищем</typeparam>
        /// <typeparam name="TS">Где оно должно находиться</typeparam>
        /// <param name="obj">Результат нашего поиска</param>
        /// <param name="source">Где оно должно находиться</param>
        /// <example>
        ///   m_ActorsManager = FindObjectOfType<ActorsManager>();
        ///   DebugUtility.HandleErrorIfNullFindObject<ActorsManager, DetectionModule>(m_ActorsManager, this);
        /// </example>
        public static void HandleErrorIfNullFindObject<TO, TS>(Object obj, Component source)
        {
            if (obj == null)
            {
                Debug.LogError($"Ошибка: Компонент типа {typeof(TS)} on GameObject {source.gameObject.name}" +
                               $" ожидалось, что вы найдете объект типа {typeof(TO)} " +
                               "на сцене, но ничего найдено не было.");
            }
        }

        /// <summary>
        /// Выводит ошибку, если ни компонент в данном контроллере не найден
        /// </summary>
        /// <typeparam name="TO">То что мы ищем</typeparam>
        /// <typeparam name="TS">Где оно должно находиться</typeparam>
        /// <param name="count">Кол-во компонентов</param>
        /// <param name="source">Где оно должно находиться</param>
        /// <param name="onObject">Сам объект, где мы это делаем</param>
        /// <example>
        ///    var detectionModules = GetComponentsInChildren<DetectionModule>();
        ///    DebugUtility.HandleErrorIfNoComponentFound<DetectionModule, EnemyController>(detectionModules.Length, this,gameObject);
        /// </example>
        public static void HandleErrorIfNoComponentFound<TO, TS>(int count, Component source, GameObject onObject)
        {
            if (count == 0)
            {
                Debug.LogError($"Ошибка: Компонент типа {typeof(TS)} on GameObject {source.gameObject.name}" +
                               $" ожидается, что будет найден хотя бы один компонент типа {typeof(TO)} on GameObject " +
                               $"{onObject.name}, но ни один из них не был найден.");
            }
        }

        /// <summary>
        /// Выводит предупреждение, если были созданы дубликаты
        /// </summary>
        /// <typeparam name="TO">То что мы ищем</typeparam>
        /// <typeparam name="TS">Где оно должно находиться</typeparam>
        /// <param name="count">Кол-во компонентов</param>
        /// <param name="source">Где оно должно находиться</param>
        /// <param name="onObject">Сам объект, где мы это делаем</param>
        /// <example>
        ///    var detectionModules = GetComponentsInChildren<DetectionModule>();
        ///     DebugUtility.HandleWarningIfDuplicateObjects<DetectionModule, EnemyController>(detectionModules.Length,this, gameObject);
        /// </example>
        public static void HandleWarningIfDuplicateObjects<TO, TS>(int count, Component source, GameObject onObject)
        {
            if (count > 1)
            {
                Debug.LogWarning($"Предупреждение: Компонент типа {typeof(TS)} on GameObject {source.gameObject.name}" +
                                 $" ожидается, что вы найдете только один компонент типа {typeof(TO)} on GameObject " +
                                  $"{onObject.name}, но несколько из них были найдены. Будет выбран первый найденный.");
            }
        }
    }
}
