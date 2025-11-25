using System.Collections.Generic;

public static class TaskDataStore
{
    public static List<TaskData> tasks = new List<TaskData>();
    public static List<MilestoneData> milestones = new List<MilestoneData>();
    public static float energyValue = 0f;

    // Ghi nhớ ngày reset cuối cùng
    public static string lastDailyResetDate = "";
}
