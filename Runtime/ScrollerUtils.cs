using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace UIS {
    public static class ScrollerUtils {
        public enum EdgeState { leftOrTop, crosses, rightOrBottom };
        public enum ContentEdge { leftOrTop, rightOrBottom };

        private static EdgeState ComputeEdgeState(float min, float max, float edge) {
            if (max < edge) {
                return EdgeState.leftOrTop;
            }
            if (min > edge) {
                return EdgeState.rightOrBottom;
            }
            return EdgeState.crosses;
        }

        public class ItemEdgeSearchResult {
            public ItemEdgeSearchResult() {
                ItemIndex = -1;
                ContentEdge = ContentEdge.leftOrTop;
                DistToEdge = float.MaxValue;
                EdgeCrossing = EdgeState.leftOrTop;
                EdgePos = 0f;
                ItemMin = float.MaxValue;
                ItemMax = float.MinValue;
                ItemSize = 0f;
                IsFirstItem = false;
                IsLastItem = false;
            }

            public int ItemIndex { get; set; }
            public ContentEdge ContentEdge { get; set; }
            public float DistToEdge { get; set; }
            public EdgeState EdgeCrossing { get; set; }
            public float EdgePos { get; set; }
            public float ItemMin { get; set; }
            public float ItemMax { get; set; }
            public float ItemSize { get; set; }
            public bool IsFirstItem { get; set; }
            public bool IsLastItem { get; set; }
        }

        /// <summary>
        /// Find the items in the content window that are closest to the left and right edges of the viewport.
        /// Also return where the item is relative to the content edge and how far.
        /// TODO: This needs to use binary search.
        /// </summary>
        /// <param name="_scroll"></param>
        /// <param name="_content"></param>
        /// <param name="_count"></param>
        /// <param name="_positions"></param>
        /// <param name="_widths"></param>
        /// <returns></returns>
        public static (ItemEdgeSearchResult left, ItemEdgeSearchResult right) ComputeIndexRangeVert(ScrollRect _scroll, RectTransform _content, int _count, Dictionary<int, float> _positions, Dictionary<int, int> _widths) {
            var contentMin = _content.anchoredPosition.y;
            var contentMax = contentMin + _scroll.viewport.rect.height;

            var closestLeftEdgeDist = new ItemEdgeSearchResult() {
                ContentEdge = ContentEdge.leftOrTop
            };

            var closestRightEdgeDist = new ItemEdgeSearchResult() {
                ContentEdge = ContentEdge.rightOrBottom
            };

            var lastIndex = _count - 1;
            for (var index = 0; index < _count; index++) {
                var min = -_positions[index];
                var currSize = _widths[index];
                var max = min + currSize;
                var deltaLeft = Mathf.Abs(min - contentMin);
                var deltaRight = Mathf.Abs(max - contentMin);
                var minDelta = Mathf.Min(deltaLeft, deltaRight);
                var isFirstIndex = index == 0;
                var isLastIndex = index == lastIndex;

                // Compare the current item with the top edge.
                if (minDelta < closestLeftEdgeDist.DistToEdge) {
                    closestLeftEdgeDist.ItemIndex = index;
                    closestLeftEdgeDist.EdgeCrossing = ComputeEdgeState(min, max, contentMin);
                    closestLeftEdgeDist.EdgePos = contentMin;
                    closestLeftEdgeDist.ItemMin = min;
                    closestLeftEdgeDist.ItemMax = max;
                    closestLeftEdgeDist.ItemSize = currSize;
                    closestLeftEdgeDist.DistToEdge = minDelta;
                    closestLeftEdgeDist.IsFirstItem = isFirstIndex;
                    closestLeftEdgeDist.IsLastItem = isLastIndex;
                }

                // Compare the current item with the bottom edge.
                deltaLeft = Mathf.Abs(min - contentMax);
                deltaRight = Mathf.Abs(max - contentMax);
                minDelta = Mathf.Min(deltaLeft, deltaRight);
                if (minDelta < closestRightEdgeDist.DistToEdge) {
                    closestRightEdgeDist.ItemIndex = index;
                    closestRightEdgeDist.EdgeCrossing = ComputeEdgeState(min, max, contentMax);
                    closestRightEdgeDist.EdgePos = contentMax;
                    closestRightEdgeDist.ItemMin = min;
                    closestRightEdgeDist.ItemMax = max;
                    closestRightEdgeDist.ItemSize = currSize;
                    closestRightEdgeDist.DistToEdge = minDelta;
                    closestRightEdgeDist.IsFirstItem = isFirstIndex;
                    closestRightEdgeDist.IsLastItem = isLastIndex;
                }
            }

            //// BEGIN DEBUG
            //if (closestLeftEdgeDist.ItemIndex != -1) {
            //    Debug.Log($"ComputeIndexRangeVert(): {closestLeftEdgeDist.ContentEdge}: [{closestLeftEdgeDist.ItemIndex}], IsFirstItem={closestLeftEdgeDist.IsFirstItem}, IsLastItem={closestLeftEdgeDist.IsLastItem}: EdgeCrossing={closestLeftEdgeDist.EdgeCrossing}, EdgePos={closestLeftEdgeDist.EdgePos}: size={closestLeftEdgeDist.ItemSize}, [min:{closestLeftEdgeDist.ItemMin}..max:{closestLeftEdgeDist.ItemMax}] (DistToEdge: {closestLeftEdgeDist.DistToEdge})");
            //}
            //if (closestRightEdgeDist.ItemIndex != -1) {
            //    Debug.Log($"ComputeIndexRangeVert(): {closestRightEdgeDist.ContentEdge}: [{closestRightEdgeDist.ItemIndex}], IsFirstItem={closestRightEdgeDist.IsFirstItem}, IsLastItem={closestRightEdgeDist.IsLastItem}: EdgeCrossing={closestRightEdgeDist.EdgeCrossing}, EdgePos={closestRightEdgeDist.EdgePos}: size={closestRightEdgeDist.ItemSize}, [min:{closestRightEdgeDist.ItemMin}..max:{closestRightEdgeDist.ItemMax}] (DistToEdge: {closestRightEdgeDist.DistToEdge})");
            //}
            //// END DEBUG

            return (closestLeftEdgeDist.ItemIndex != -1 ? closestLeftEdgeDist : null, closestRightEdgeDist.ItemIndex != -1 ? closestRightEdgeDist : null);
        }

        /// <summary>
        /// Find the items in the content window that are closest to the left and right edges of the viewport.
        /// Also return where the item is relative to the content edge and how far.
        /// TODO: This needs to use binary search.
        /// </summary>
        /// <param name="_scroll"></param>
        /// <param name="_content"></param>
        /// <param name="_count"></param>
        /// <param name="_positions"></param>
        /// <param name="_widths"></param>
        /// <returns></returns>
        public static (ItemEdgeSearchResult left, ItemEdgeSearchResult right) ComputeIndexRangeHorz(ScrollRect _scroll, RectTransform _content, int _count, Dictionary<int, float> _positions, Dictionary<int, int> _widths) {
            var viewportMin = _scroll.viewport.anchoredPosition.x;
            var viewportMax = viewportMin + _scroll.viewport.rect.width;

            var contentMin = _content.anchoredPosition.x;
            //var contentMax = contentMin + _content.rect.width;

            var closestLeftEdgeDist = new ItemEdgeSearchResult() {
                ContentEdge = ContentEdge.leftOrTop
            };

            var closestRightEdgeDist = new ItemEdgeSearchResult() {
                ContentEdge = ContentEdge.rightOrBottom
            };

            var lastIndex = _count - 1;
            for (var index = 0; index < _count; index++) {
                var min = _positions[index] + contentMin;
                var currSize = _widths[index];
                var max = min + currSize;
                var deltaLeft = Mathf.Abs(min - viewportMin);
                var deltaRight = Mathf.Abs(max - viewportMin);
                var minDelta = Mathf.Min(deltaLeft, deltaRight);
                var isFirstIndex = index == 0;
                var isLastIndex = index == lastIndex;

                // Compare with the left edge.
                if (minDelta < closestLeftEdgeDist.DistToEdge) {
                    closestLeftEdgeDist.ItemIndex = index;
                    closestLeftEdgeDist.EdgeCrossing = ComputeEdgeState(min, max, viewportMin);
                    closestLeftEdgeDist.ItemMin = min;
                    closestLeftEdgeDist.ItemMax = max;
                    closestLeftEdgeDist.ItemSize = currSize;
                    closestLeftEdgeDist.DistToEdge = minDelta;
                    closestLeftEdgeDist.IsFirstItem = isFirstIndex;
                    closestLeftEdgeDist.IsLastItem = isLastIndex;
                }

                // Compare with the right edge.
                deltaLeft = Mathf.Abs(min - viewportMax);
                deltaRight = Mathf.Abs(max - viewportMax);
                minDelta = Mathf.Min(deltaLeft, deltaRight);
                if (minDelta < closestRightEdgeDist.DistToEdge) {
                    closestRightEdgeDist.ItemIndex = index;
                    closestRightEdgeDist.EdgeCrossing = ComputeEdgeState(min, max, viewportMax);
                    closestRightEdgeDist.ItemMin = min;
                    closestRightEdgeDist.ItemMax = max;
                    closestRightEdgeDist.ItemSize = currSize;
                    closestRightEdgeDist.DistToEdge = minDelta;
                    closestRightEdgeDist.IsFirstItem = isFirstIndex;
                    closestRightEdgeDist.IsLastItem = isLastIndex;
                }
            }

            //// BEGIN DEBUG
            //if (closestLeftEdgeDist.ItemIndex != -1) {
            //    Debug.Log($"ComputeIndexRangeHorz(): {closestLeftEdgeDist.ContentEdge}: [{closestLeftEdgeDist.ItemIndex}], IsFirstItem={closestLeftEdgeDist.IsFirstItem}, IsLastItem={closestLeftEdgeDist.IsLastItem}: EdgeCrossing={closestLeftEdgeDist.EdgeCrossing}: size={closestLeftEdgeDist.ItemSize}, [min:{closestLeftEdgeDist.ItemMin}..max:{closestLeftEdgeDist.ItemMax}] (DistToEdge: {closestLeftEdgeDist.DistToEdge})");
            //}
            //if (closestRightEdgeDist.ItemIndex != -1) {
            //    Debug.Log($"ComputeIndexRangeHorz(): {closestRightEdgeDist.ContentEdge}: [{closestRightEdgeDist.ItemIndex}], IsFirstItem={closestRightEdgeDist.IsFirstItem}, IsLastItem={closestRightEdgeDist.IsLastItem}: EdgeCrossing={closestRightEdgeDist.EdgeCrossing}: size={closestRightEdgeDist.ItemSize}, [min:{closestRightEdgeDist.ItemMin}..max:{closestRightEdgeDist.ItemMax}] (DistToEdge: {closestRightEdgeDist.DistToEdge})");
            //}
            //// END DEBUG

            return (closestLeftEdgeDist.ItemIndex != -1 ? closestLeftEdgeDist : null, closestRightEdgeDist.ItemIndex != -1 ? closestRightEdgeDist : null);
        }
    }
}
