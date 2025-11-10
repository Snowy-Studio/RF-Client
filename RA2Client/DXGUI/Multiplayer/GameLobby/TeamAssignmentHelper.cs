using System;
using System.Collections.Generic;
using System.Linq;
using Ra2Client.Domain.Multiplayer;

namespace Ra2Client.DXGUI.Multiplayer.GameLobby
{


    public static class TeamAssignmentHelper
    {
        // 使用并查集合并玩家
        private static int Find(int[] parent, int x)
        {
            if (parent[x] != x)
                parent[x] = Find(parent, parent[x]);
            return parent[x];
        }

        // 合并两个玩家到同一个组
        private static void Union(int[] parent, int[] rank, int a, int b)
        {
            int rootA = Find(parent, a);
            int rootB = Find(parent, b);

            if (rootA != rootB)
            {
                // 使用秩优化，合并较小的树
                if (rank[rootA] > rank[rootB])
                    parent[rootB] = rootA;
                else if (rank[rootA] < rank[rootB])
                    parent[rootA] = rootB;
                else
                {
                    parent[rootB] = rootA;
                    rank[rootA]++;
                }
            }
        }

        public static Dictionary<int, int> Assign(int playerCount, List<Rule> rules, int maxTeams = 4)
        {
            // 初始化并查集
            int[] parent = Enumerable.Range(0, playerCount).ToArray();
            int[] rank = new int[playerCount];

            // 处理必须同队的规则
            foreach (var rule in rules.Where(r => r.Requirement == PositionRequirement.SameTeam))
            {
                Union(parent, rank, rule.Position1, (int)rule.Position2);
            }

            // 处理必须不同队的规则
            Dictionary<int, List<int>> differentTeamPairs = new();
            foreach (var rule in rules.Where(r => r.Requirement == PositionRequirement.DifferentTeam))
            {
                int root1 = Find(parent, rule.Position1);
                int root2 = Find(parent, (int)rule.Position2);

                if (!differentTeamPairs.ContainsKey(root1))
                    differentTeamPairs[root1] = new List<int>();
                if (!differentTeamPairs.ContainsKey(root2))
                    differentTeamPairs[root2] = new List<int>();

                differentTeamPairs[root1].Add(root2);
                differentTeamPairs[root2].Add(root1);
            }

            // 队伍分配：每个组（通过并查集找到的组）会被分配一个队伍
            Dictionary<int, int> groupToTeam = new();
            int nextTeam = 1;

            // 用队伍分配玩家
            int[] playerTeams = new int[playerCount];
            for (int i = 0; i < playerCount; i++)
            {
                playerTeams[i] = -1; // 初始化所有玩家的队伍
            }

            // 处理所有玩家的同队与不同队关系
            foreach (var pair in differentTeamPairs)
            {
                int team = nextTeam++;
                if (team > maxTeams)
                    throw new Exception("团队数量超过限制");

                // 给每对玩家分配不同队伍
                foreach (var player in pair.Value)
                {
                    playerTeams[player] = team;
                    int root = Find(parent, player);
                    if (!groupToTeam.ContainsKey(root))
                        groupToTeam[root] = team;
                }
            }

            // 处理没有被分配队伍的玩家，默认分配到队伍 0
            for (int i = 0; i < playerCount; i++)
            {
                if (playerTeams[i] == -1)
                {
                    playerTeams[i] = 0; // 默认队伍是 0
                }
            }

            // 返回每个玩家的团队分配
            return Enumerable.Range(0, playerCount)
                .ToDictionary(i => i, i => groupToTeam.GetValueOrDefault(Find(parent, i), 0));

        }
    }



}

