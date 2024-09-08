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
            public SearchParams(int minIndex, int maxIndex, float searchPos, int itemCount, float posOffset, bool negatePositions, Dictionary<int, float> positions, Dictionary<int, int> itemSizes) {
                MinIndex = minIndex;
                MaxIndex = maxIndex;
                SearchPos = searchPos;
                ItemCount = itemCount;
                PosOffset = posOffset;
                NegatePositions = negatePositions;
                Positions = positions;
                ItemSizes = itemSizes;
            }

            public int MinIndex { get; set; }
            public int MaxIndex { get; set; }
            public float SearchPos { get; set; }
            public int ItemCount { get; set; }
            public float PosOffset { get; set; }
            public bool NegatePositions { get; set; }
            public Dictionary<int, float> Positions { get; set; }
            public Dictionary<int, int> ItemSizes { get; set; }
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
        /// <param name="heights"></param>
        /// <returns></returns>
        public static (ItemEdgeSearchResult leftOrTop, ItemEdgeSearchResult rightOrBottom) ComputeIndexRangeVert(
                                            ScrollRect scroll, RectTransform content, int count,
                                            Dictionary<int, float> positions, Dictionary<int, int> heights,
                                            bool searchLeft = true, bool searchRight = true,
                                            CancellationToken ct = default) {

            var contentMin = content.anchoredPosition.y;
            var contentMax = contentMin + scroll.viewport.rect.height;

            // Search Left
            var searchParamsLeft = new SearchParams(minIndex: 0, maxIndex: count - 1, searchPos: contentMin, itemCount: count, posOffset: 0f, negatePositions: true, positions: positions, itemSizes: heights);
            var resultLeft = searchLeft ? FindClosestEdges(searchParamsLeft, ct: ct) : null;

            //// DEBUG LOG
            //if (resultLeft != null) {
            //    Debug.Log($"ComputeIndexRangeVert2(): resultLeft: ItemIndex={resultLeft.ItemIndex}, ContentEdge={resultLeft.ContentEdge}, DistToEdge={resultLeft.DistToEdge}, EdgeCrossing={resultLeft.EdgeCrossing}, ItemSize={resultLeft.ItemSize}, EdgePos={resultLeft.EdgePos}, ItemMin={resultLeft.ItemMin}, ItemMax={resultLeft.ItemMax}, SearchDepth={resultLeft.SearchDepth}, ItemCount={resultLeft.ItemCount}, {resultLeft.SearchDepth}");
            //}

            // Search Right
            var searchParamsRight = new SearchParams(minIndex: 0, maxIndex: count - 1, searchPos: contentMax, itemCount: count, posOffset: 0f, negatePositions: true, positions: positions, itemSizes: heights);
            var resultRight = searchRight ? FindClosestEdges(searchParamsRight, ct: ct) : null;

            //// DEBUG LOG
            //if (resultRight != null) {
            //    Debug.Log($"ComputeIndexRangeVert2(): resultRight: ItemIndex={resultRight.ItemIndex}, ContentEdge={resultRight.ContentEdge}, DistToEdge={resultRight.DistToEdge}, EdgeCrossing={resultRight.EdgeCrossing}, ItemSize={resultRight.ItemSize}, EdgePos={resultRight.EdgePos}, ItemMin={resultRight.ItemMin}, ItemMax={resultRight.ItemMax}, SearchDepth={resultRight.SearchDepth}, ItemCount={resultLeft.ItemCount}, {resultLeft.SearchDepth}");
            //}

            return (resultLeft?.ItemIndex != -1 ? resultLeft : null, resultRight?.ItemIndex != -1 ? resultRight : null);
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
        public static (ItemEdgeSearchResult left, ItemEdgeSearchResult right) ComputeIndexRangeHorz(
                                                    ScrollRect scroll, RectTransform content, int count, 
                                                    Dictionary<int, float> positions, Dictionary<int, int> widths,
                                                    bool searchLeft = true, bool searchRight = true,
                                                    CancellationToken ct = default) {
            var viewportMin = scroll.viewport.anchoredPosition.x;
            var viewportMax = viewportMin + scroll.viewport.rect.width;

            var contentMin = content.anchoredPosition.x;
            //var contentMax = contentMin + _content.rect.width;

            // Search Left
            var searchParamsLeft = new SearchParams(minIndex: 0, maxIndex: count - 1, searchPos: viewportMin, itemCount: count, posOffset: contentMin, negatePositions: false, positions: positions, itemSizes: widths);
            var resultLeft = searchLeft ? FindClosestEdges(searchParamsLeft, ct: ct) : null;

            // If the left result is to the right of the edge, move the left edge index one step to the left.
            if ((resultLeft != null) && (resultLeft.EdgeCrossing == EdgeState.rightOrBottom)) {
                resultLeft.ItemIndex = Mathf.Max(0, (resultLeft.ItemIndex - 1));
            }

            // DEBUG LOG
            //if (resultLeft != null) {
            //    Debug.Log($"FindClosestEdges(): resultLeft: ItemIndex={resultLeft.ItemIndex}, ContentEdge={resultLeft.ContentEdge}, DistToEdge={resultLeft.DistToEdge}, EdgeCrossing={resultLeft.EdgeCrossing}, ItemSize={resultLeft.ItemSize}, EdgePos={resultLeft.EdgePos}, ItemMin={resultLeft.ItemMin}, ItemMax={resultLeft.ItemMax}, SearchDepth={resultLeft.SearchDepth}, ItemCount={searchParamsLeft.ItemCount}");
            //}

            // Search Right
            var searchParamsRight = new SearchParams(minIndex: 0, maxIndex: count - 1, searchPos: viewportMax, itemCount: count, posOffset: contentMin, negatePositions: false, positions: positions, itemSizes: widths);
            var resultRight = searchRight ? FindClosestEdges(searchParamsRight, ct: ct) : null;

            // If the right result is the left of the edge, move the right edge one step to the right.
            if ((resultRight != null) && (resultRight.EdgeCrossing == EdgeState.leftOrTop)) {
                resultRight.ItemIndex = Mathf.Min(count - 1, resultRight.ItemIndex + 1);
            }

            // DEBUG LOG
            //if (resultRight != null) {
            //    Debug.Log($"FindClosestEdges(): resultRight: ItemIndex={resultRight.ItemIndex}, ContentEdge={resultRight.ContentEdge}, DistToEdge={resultRight.DistToEdge}, EdgeCrossing={resultRight.EdgeCrossing}, ItemSize={resultRight.ItemSize}, EdgePos={resultRight.EdgePos}, ItemMin={resultRight.ItemMin}, ItemMax={resultRight.ItemMax}, SearchDepth={resultRight.SearchDepth}, ItemCount={searchParamsRight.ItemCount}");
            //}

            return (resultLeft?.ItemIndex != -1 ? resultLeft : null, resultRight?.ItemIndex != -1 ? resultRight : null);
        }

        /// <summary>
        /// Use binary search to find the closest item to the given edge specified in the search params.
        /// Note: This function is recursive.
        /// </summary>
        /// <param name="searchParams"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        static ItemEdgeSearchResult FindClosestEdges(SearchParams searchParams, int depth = 0, CancellationToken ct = default) {

            if (depth >= MAX_SEACH_DEPTH) {
                Debug.LogError($"FindClosestEdges(): Max. search depth reached! depth={depth}, MAX_SEARCH_DEPTH={MAX_SEACH_DEPTH}");
                return null;
            }

            if ((searchParams.Positions.Count != searchParams.ItemCount) || (searchParams.ItemSizes.Count != searchParams.ItemCount)) {
                Debug.LogError($"FindClosestEdges(): Positions or Widths array not fully up-to-date: searchParams.Count={searchParams.ItemCount}: searchParams.Positions.Count={searchParams.Positions.Count}, searchParams.Widths.Count={searchParams.ItemSizes.Count}");
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
            var negatePosition = searchParams.NegatePositions ? -1f : 1f;
            var widths = searchParams.ItemSizes;

            var minPos = (negatePosition * positions[minIndex]) + posOffset;

            var maxIndexWidth = widths[maxIndex];
            var maxPos = (negatePosition * positions[maxIndex]) + maxIndexWidth;

            // Check for edge case where the search position is fully outside the current search interval.
            // If outside, the first or last item is the closest item.
            if (searchPos < minPos) {
                var resultMinPos = new ItemEdgeSearchResult {
                    ItemIndex = minIndex,
                    EdgeCrossing = ComputeEdgeState(minPos, maxPos, searchPos),
                    ItemMin = minPos,
                    ItemMax = maxPos,
                    ItemSize = widths[minIndex],
                    DistToEdge = minPos - searchPos,
                    EdgePos = searchPos,
                    IsFirstItem = minIndex == 0,
                    IsLastItem = minIndex == (count - 1),
                    SearchDepth = depth,
                    ItemCount = searchParams.ItemCount
                };
                return resultMinPos;
            } else if (searchPos > maxPos) {
                var resultMaxPos = new ItemEdgeSearchResult {
                    ItemIndex = maxIndex,
                    EdgeCrossing = ComputeEdgeState(minPos, maxPos, searchPos),
                    ItemMin = minPos,
                    ItemMax = maxPos,
                    ItemSize = widths[maxIndex],
                    DistToEdge = searchPos - maxPos,
                    EdgePos = searchPos,
                    IsFirstItem = maxIndex == 0,
                    IsLastItem = maxIndex == (count - 1),
                    SearchDepth = depth,
                    ItemCount = searchParams.ItemCount
                };
                return resultMaxPos;
            }

            if ((minIndex < 0) || (maxIndex >= count) || (minIndex > maxIndex) || ((searchPos < minPos) && minIsFirstIndex) || ((searchPos > maxPos) && maxIsLastIndex)) {
                Debug.LogError($"FindClosestEdges(): Invalid range! minIndex={minIndex}, minIsFirstIndex={minIsFirstIndex}, maxIndex={maxIndex}, maxIslastIndex={maxIsLastIndex}, minPos={minPos}, maxPos={maxPos}, maxIndexWidth={maxIndexWidth}, searchPos={searchPos}, count={count}");
                return null;
            }

            var index = (minIndex + maxIndex) / 2;
            var min = (negatePosition * positions[index]) + posOffset;
            var currSize = widths[index];
            var max = min + currSize;
            if (Mathf.Abs(minIndex - maxIndex) == 1) {
                // The search partion has been reduced to 1. So, the solution is one of these.
                // Because of truncation from the averaging (i.e., (minIndex + maxIndex) / 2),
                // maxIndex is the *other* index for which we need search distance information.

                // Solution Found: Index is the closest to the edge. (MinIndex==MaxIndex)
                var minIndexDist = DistToEdge(searchParams.MinIndex, searchParams);
                var maxIndexDist = DistToEdge(searchParams.MaxIndex, searchParams);
                var (closestIndex, closestDist) = (minIndexDist < maxIndexDist) ? (minIndex, minIndexDist) : (maxIndex, maxIndexDist);

                min = (negatePosition * positions[closestIndex]) + posOffset;
                currSize = widths[closestIndex];
                max = min + currSize;

                var resultMinmax = new ItemEdgeSearchResult {
                    ItemIndex = closestIndex,
                    EdgeCrossing = ComputeEdgeState(min, max, searchPos),
                    ItemMin = min,
                    ItemMax = max,
                    ItemSize = currSize,
                    DistToEdge = closestDist,
                    EdgePos = searchPos,
                    IsFirstItem = (closestIndex == 0),
                    IsLastItem = (closestIndex == (count - 1)),
                    SearchDepth = depth,
                    ItemCount = searchParams.ItemCount
                };
                return resultMinmax;
            } else if (searchPos < min) {
                // Recurse on the right partition
                searchParams.MaxIndex = Mathf.Max(index, searchParams.MinIndex);
                if (searchParams.MinIndex != searchParams.MaxIndex) {
                    return FindClosestEdges(searchParams, depth + 1, ct);
                }
                // Solution Found: Index is the closest to the edge. (MinIndex==MaxIndex)
            } else if (searchPos > max) {
                // Recurse on the right partition
                searchParams.MinIndex = Mathf.Min(index, searchParams.MaxIndex);
                if (searchParams.MinIndex != searchParams.MaxIndex) {
                    return FindClosestEdges(searchParams, depth + 1, ct);
                }
                // Solution Found: Index is the closest to the edge. (MinIndex==MaxIndex)
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
                EdgePos = searchPos,
                IsFirstItem = isFirstIndex,
                IsLastItem = isLastIndex,
                SearchDepth = depth,
                ItemCount = searchParams.ItemCount
            };

            return result;
        }

        static float DistToEdge(int index, SearchParams searchParams) {
            var searchPos = searchParams.SearchPos;
            var posOffset = searchParams.PosOffset;
            var positions = searchParams.Positions;
            var negatePosition = searchParams.NegatePositions ? -1f : 1f;
            var widths = searchParams.ItemSizes;

            var minPos = (negatePosition * positions[index]) + posOffset;

            var maxIndexWidth = widths[index];
            var maxPos = (negatePosition * positions[index]) + maxIndexWidth;

            var deltaLeft = Mathf.Abs(minPos - searchPos);
            var deltaRight = Mathf.Abs(maxPos - searchPos);
            var minDelta = Mathf.Min(deltaLeft, deltaRight);
            return minDelta;
        }
    }
}
