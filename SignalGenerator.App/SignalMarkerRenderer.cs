using System;
using System.Collections.Generic;
using ScottPlot;
using SignalProcessing.Core;

namespace SignalGenerator.App
{
    /// <summary>
    /// Управляет отображением и удалением графических маркеров пересечений на графике ScottPlot.
    /// </summary>
    public sealed class SignalMarkerRenderer
    {
        private readonly Plot _plot;
        private readonly List<IPlottable> _markers = new List<IPlottable>();

        /// <summary>
        /// Создаёт объект отображения маркеров для указанного графика.
        /// </summary>
        /// <param name="plot">График ScottPlot, на котором размещаются маркеры.</param>
        /// <exception cref="ArgumentNullException"><paramref name="plot"/> равен null.</exception>
        public SignalMarkerRenderer(Plot plot)
        {
            _plot = plot ?? throw new ArgumentNullException(nameof(plot), "График не может быть null.");
        }

        /// <summary>
        /// Добавляет маркер на график в точке (time, value) без перерисовки графика.
        /// </summary>
        /// <param name="time">Момент времени отсчёта, с.</param>
        /// <param name="value">Значение сигнала в отсчёте.</param>
        /// <param name="direction">Направление пересечения порога (вверх — зелёный, вниз — красный).</param>
        public void AddMarker(float time, float value, CrossingDirection direction)
        {
            var marker = _plot.Add.Marker(time, value);
            marker.Color = direction == CrossingDirection.Up ? Colors.Green : Colors.Red;
            marker.Size = 9;

            _markers.Add(marker);
        }

        /// <summary>
        /// Удаляет с графика только добавленные маркеры, не затрагивая кривые сигналов.
        /// </summary>
        public void ClearMarkers()
        {
            foreach (IPlottable marker in _markers)
            {
                _plot.Remove(marker);
            }

            _markers.Clear();
        }
    }
}