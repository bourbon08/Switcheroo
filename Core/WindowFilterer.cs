﻿/*
 * Switcheroo - The incremental-search task switcher for Windows.
 * http://www.switcheroo.io/
 * Copyright 2009, 2010 James Sulak
 * Copyright 2014 Regin Larsen
 * 
 * Switcheroo is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Switcheroo is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with Switcheroo.  If not, see <http://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using System.Linq;
using Switcheroo.Core.Matchers;

namespace Switcheroo.Core
{
    public class WindowFilterer
    {
        public IEnumerable<FilterResult<T>> Filter<T>(WindowFilterContext<T> context, string query) where T : IWindowText
        {
            var filterText = query;
            string processFilterText = null;

            var queryParts = query.Split(new[] { '.' }, 2);

            //foreach (var part in queryParts)
            //{
            //    System.Console.WriteLine(part);
            //}


            // 长度为1时， processFilterText为null
            if (queryParts.Length == 2)
            {
                // todo 除了最后一个
                processFilterText = queryParts[0].Trim();
                if (processFilterText.Length == 0)
                {
                    // 过滤
                    processFilterText = context.ForegroundWindowProcessTitle.Trim();
                }

                // 输入框字符
                filterText = queryParts[1].Trim();
            }

            var step1 = context.Windows
                .Select(
                    w =>
                    {
                        return new
                        {
                            Window = w,
                            ResultsTitle = Score(w.WindowTitle, filterText),
                            ResultsProcessTitle = Score(w.ProcessTitle, filterText)
                        };
                    });

            return step1.Where(r =>
            {
                if (processFilterText == null)
                {
                    return r.ResultsTitle.Any(wt => wt.Matched) || r.ResultsProcessTitle.Any(pt => pt.Matched);
                }
                else
                {
                    return r.ResultsTitle.Any(wt => wt.Matched) && r.ResultsProcessTitle.Any(pt => pt.Matched);
                }
            })
            .OrderByDescending(r => r.ResultsTitle.Sum(wt => wt.Score) + r.ResultsProcessTitle.Sum(pt => pt.Score))
            .Select(
                r =>
                    new FilterResult<T>
                    {
                        AppWindow = r.Window,
                        WindowTitleMatchResults = r.ResultsTitle,
                        ProcessTitleMatchResults = r.ResultsProcessTitle
                    });
        }

        private static List<MatchResult> Score(string title, string filterText)
        {
            var startsWithMatcher = new StartsWithMatcher();
            var containsMatcher = new ContainsMatcher();
            var significantCharactersMatcher = new SignificantCharactersMatcher();
            var individualCharactersMatcher = new IndividualCharactersMatcher();

            var results = new List<MatchResult>
            {
                startsWithMatcher.Evaluate(title, filterText),
                significantCharactersMatcher.Evaluate(title, filterText),
                containsMatcher.Evaluate(title, filterText),
                individualCharactersMatcher.Evaluate(title, filterText)
            };

            return results;
        }
    }

    public class WindowFilterContext<T> where T : IWindowText
    {
        public string ForegroundWindowProcessTitle { get; set; }
        public IEnumerable<T> Windows { get; set; }
    }
}