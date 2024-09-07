using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;


namespace UIS {
    public static class ScrollerUtils {
        const int MAX_SEACH_DEPTH = 12; // Note: this should never be hit but is here as 1 of 2 emergency exits for the recursive search. (The other exit is the cancellation token.)

        public enum EdgeState { leftOrTop, crosses, rightOrBottom };
        public enum ContentEdge { leftOrTop, rightOrBottom };

        static EdgeState ComputeEdgeState(float min, float max, float edge) {
            if (max < edge) {
                return EdgeState.leftOrTop;
            }
            if (min > edge) {
                return EdgeState.rightOrBottom;
            }
            return EdgeState.crosses;
        }

        class SearchParams {
            public SearchParams(int minIndex, int maxIndex, float searchPos, int itemCount, float posOffset, Dictionary<int, float> positions, Dictionary<int, int> widths) {
                MinIndex = minIndex;
                MaxIndex = maxIndex;
                SearchPos = searchPos;
                ItemCount = itemCount;
                PosOffset = posOffset;
                Positions = positions;
                Widths = widths;
            }

            public int MinIndex { get; set; }
            public int MaxIndex { get; set; }
            public float SearchPos { get; set; }
            public int ItemCount { get; set; }
            public float PosOffset { get; set; }
            public Dictionary<int, float> Positions { get; set; }
            public Dictionary<int, int> Widths { get; set; }
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
                SearchDepth = 0;
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
            public int SearchDepth { get; set; }
            public int ItemCount { get; set; }
        }

        /// <summary>
        /// Find the items in the content window that are closest to the left and right edges of the viewport.
        /// Also return where the item is relative to the content edge and how far.
        /// TODO: This needs to use binary search.
        /// </summary>
        /// <param name="scroll"></param>
        /// <param name="content"></param>
        /// <param name="count"></param>
        /// <param name="positions"></param>
        /// <param name="widths"></param>
        /// <returns></returns>
        public static (ItemEdgeSearchResult leftOrTop, ItemEdgeSearchResult rightOrBottom) ComputeIndexRangeVert(
                                            ScrollRect scroll, RectTransform content, int count, 
                                            Dictionary<int, float> positions, Dictionary<int, int> widths) {
            var contentMin = content.anchoredPosition.y;
            var contentMax = contentMin + scroll.viewport.rect.height;

            var closestLeftEdgeDist = new ItemEdgeSearchResult() {
                ContentEdge = ContentEdge.leftOrTop
            };

            var closestRightEdgeDist = new ItemEdgeSearchResult() {
                ContentEdge = ContentEdge.rightOrBottom
            };

            var lastIndex = count - 1;
            for (var index = 0; index < count; index++) {
                var min = -positions[index];
                var currSize = widths[index];
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
        public static (ItemEdgeSearchResult left, ItemEdgeSearchResult right) ComputeIndexRangeHorz(ScrollRect _scroll, RectTransform _content, int _count, Dictionary<int, float> _positions, Dictionary<int, int> _widths, CancellationToken ct = default) {
            var viewportMin = _scroll.viewport.anchoredPosition.x;
            var viewportMax = viewportMin + _scroll.viewport.rect.width;

            var contentMin = _content.anchoredPosition.x;
            //var contentMax = contentMin + _content.rect.width;

            // Search Left
            var searchParamsLeft = new SearchParams(0, _count - 1, viewportMin, _count, contentMin, _positions, _widths);
            var resultLeft = FindClosestEdges(searchParamsLeft, ct: ct);

            // DEBUG LOG
            //if (resultLeft != null) {
            //    Debug.Log($"FindClosestEdges(): resultLeft: ItemIndex={resultLeft.ItemIndex}, ContentEdge={resultLeft.ContentEdge}, DistToEdge={resultLeft.DistToEdge}, EdgeCrossing={resultLeft.EdgeCrossing}, ItemSize={resultLeft.ItemSize}, EdgePos={resultLeft.EdgePos}, ItemMin={resultLeft.ItemMin}, ItemMax={resultLeft.ItemMax}, SearchDepth={resultLeft.SearchDepth}, ItemCount={searchParamsLeft.ItemCount}");
            //}

            // Search Right
            var searchParamsRight = new SearchParams(0, _count - 1, viewportMax, _count, contentMin, _positions, _widths);
            var resultRight = FindClosestEdges(searchParamsRight, ct: ct);

            // DEBUG LOG
            //if (resultRight != null) {
            //    Debug.Log($"FindClosestEdges(): resultRight: ItemIndex={resultRight.ItemIndex}, ContentEdge={resultRight.ContentEdge}, DistToEdge={resultRight.DistToEdge}, EdgeCrossing={resultRight.EdgeCrossing}, ItemSize={resultRight.ItemSize}, EdgePos={resultRight.EdgePos}, ItemMin={resultRight.ItemMin}, ItemMax={resultRight.ItemMax}, SearchDepth={resultRight.SearchDepth}, ItemCount={searchParamsRight.ItemCount}");
            //}

            return (resultLeft.ItemIndex != -1 ? resultLeft : null, resultRight.ItemIndex != -1 ? resultRight : null);
        }

        /// <summary>
        /// Use binary search to find the closest item to the given edge specified in the search params.
        /// </summary>
        /// <param name="searchParams"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        static ItemEdgeSearchResult FindClosestEdges(SearchParams searchParams, int depth = 0, CancellationToken ct = default) {

            if (depth >= MAX_SEACH_DEPTH) {
                Debug.LogError($"FindClosestEdges(): Max. search depth reached! depth={depth}, MAX_SEARCH_DEPTH={MAX_SEACH_DEPTH}");
                return null;
            }

            if ((searchParams.Positions.Count != searchParams.ItemCount) || (searchParams.Widths.Count != searchParams.ItemCount)) {
                Debug.LogError($"FindClosestEdges(): Positions or Widths array not fully up-to-date: searchParams.Count={searchParams.ItemCount}: searchParams.Positions.Count={searchParams.Positions.Count}, searchParams.Widths.Count={searchParams.Widths.Count}");
                return null;
            }

            if (ct.IsCancellationRequested) {
                return null;
            }

            var minIndex = searchParams.MinIndex;
            var minIsFirstIndex = minIndex == 0;
            var maxIndex = searchParams.MaxIndex;
            var maxIsLastIndex = maxIndex == searchParams.ItemCount - 1;
            var searchPos = searchParams.SearchPos;
            var count = searchParams.ItemCount;
            var posOffset = searchParams.PosOffset;
            var positions = searchParams.Positions;
            var widths = searchParams.Widths;

            var minPos = positions[minIndex] + posOffset;

            var maxIndexWidth = widths[maxIndex];
            var maxPos = positions[maxIndex] + maxIndexWidth;

            if ((minIndex < 0) || (maxIndex >= count) || (minIndex > maxIndex) || ((searchPos < minPos) && !minIsFirstIndex) || ((searchPos > maxPos) && !maxIsLastIndex)) {
                Debug.LogError($"FindClosestEdges(): Invalid range! minIndex={minIndex}, minIsFirstIndex={minIsFirstIndex}, maxIndex={maxIndex}, maxIslastIndex={maxIsLastIndex}, minPos={minPos}, maxPos={maxPos}, maxIndexWidth={maxIndexWidth}, searchPos={searchPos}, count={count}");
                return null;
            }

            var index = (minIndex + maxIndex) / 2;
            var min = positions[index] + posOffset;
            var currSize = widths[index];
            var max = min + currSize;
            if (searchPos < min) {
                // Recurse on the left partition.
                searchParams.MaxIndex = Mathf.Max(index - 1, searchParams.MinIndex);
                if (searchParams.MinIndex != searchParams.MaxIndex) {
                    return FindClosestEdges(searchParams, depth + 1, ct);
                }
                // Solution Found: Index is the closest to the edge.
            } else if (searchPos > max) {
                // Recurse on the right partition
                searchParams.MinIndex = Mathf.Min(index + 1, searchParams.MaxIndex);
                if (searchParams.MinIndex != searchParams.MaxIndex) {
                    return FindClosestEdges(searchParams, depth + 1, ct);
                }
                // Solution Found: Index is the closest to the edge.
            } else {
                // Solution Found: Intersection! We found it!
            }

            // SOLUTION FOUND!
            // The item at index is the closest edge or an intersection to the given edge position.
            var deltaLeft = Mathf.Abs(min - searchPos);
            var deltaRight = Mathf.Abs(max - searchPos);
            var minDelta = Mathf.Min(deltaLeft, deltaRight);

            var lastIndex = count - 1;
            var isFirstIndex = index == 0;
            var isLastIndex = index == lastIndex;

            var result = new ItemEdgeSearchResult {
                ItemIndex = index,
                EdgeCrossing = ComputeEdgeState(min, max, searchPos),
                ItemMin = min,
                ItemMax = max,
                ItemSize = currSize,
                DistToEdge = minDelta,
                IsFirstItem = isFirstIndex,
                IsLastItem = isLastIndex,
                SearchDepth = depth,
                ItemCount = searchParams.ItemCount
            };

            return result;
        }
    }
}
