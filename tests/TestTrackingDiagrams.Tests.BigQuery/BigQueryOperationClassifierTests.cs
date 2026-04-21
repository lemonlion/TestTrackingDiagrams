using TestTrackingDiagrams.Extensions.BigQuery;

namespace TestTrackingDiagrams.Tests.BigQuery;

public class BigQueryOperationClassifierTests
{
    private const string BaseUrl = "https://bigquery.googleapis.com/bigquery/v2/projects/my-project";

    // ──────────────────────────────────────────────────────────
    //  Query operations
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void Classify_PostQueries_ReturnsQuery()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/queries");

        var result = BigQueryOperationClassifier.Classify(request);

        Assert.Equal(BigQueryOperation.Query, result.Operation);
        Assert.Equal("query", result.ResourceType);
        Assert.Equal("my-project", result.ProjectId);
    }

    [Fact]
    public void Classify_GetQueryResults_ReturnsQuery()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/queries/job123");

        var result = BigQueryOperationClassifier.Classify(request);

        Assert.Equal(BigQueryOperation.Query, result.Operation);
        Assert.Equal("query", result.ResourceType);
        Assert.Equal("job123", result.ResourceName);
    }

    // ──────────────────────────────────────────────────────────
    //  Dataset operations
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void Classify_GetDatasets_ReturnsList()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/datasets");

        var result = BigQueryOperationClassifier.Classify(request);

        Assert.Equal(BigQueryOperation.List, result.Operation);
        Assert.Equal("dataset", result.ResourceType);
    }

    [Fact]
    public void Classify_PostDatasets_ReturnsCreate()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/datasets");

        var result = BigQueryOperationClassifier.Classify(request);

        Assert.Equal(BigQueryOperation.Create, result.Operation);
        Assert.Equal("dataset", result.ResourceType);
    }

    [Fact]
    public void Classify_GetDataset_ReturnsRead()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/datasets/mydataset");

        var result = BigQueryOperationClassifier.Classify(request);

        Assert.Equal(BigQueryOperation.Read, result.Operation);
        Assert.Equal("dataset", result.ResourceType);
        Assert.Equal("mydataset", result.ResourceName);
        Assert.Equal("mydataset", result.DatasetId);
    }

    [Fact]
    public void Classify_DeleteDataset_ReturnsDelete()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/datasets/mydataset");

        var result = BigQueryOperationClassifier.Classify(request);

        Assert.Equal(BigQueryOperation.Delete, result.Operation);
        Assert.Equal("dataset", result.ResourceType);
        Assert.Equal("mydataset", result.ResourceName);
    }

    [Fact]
    public void Classify_PatchDataset_ReturnsUpdate()
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"{BaseUrl}/datasets/mydataset");

        var result = BigQueryOperationClassifier.Classify(request);

        Assert.Equal(BigQueryOperation.Update, result.Operation);
        Assert.Equal("dataset", result.ResourceType);
        Assert.Equal("mydataset", result.ResourceName);
    }

    [Fact]
    public void Classify_PutDataset_ReturnsUpdate()
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"{BaseUrl}/datasets/mydataset");

        var result = BigQueryOperationClassifier.Classify(request);

        Assert.Equal(BigQueryOperation.Update, result.Operation);
        Assert.Equal("dataset", result.ResourceType);
    }

    // ──────────────────────────────────────────────────────────
    //  Table operations
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void Classify_GetTables_ReturnsList()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/datasets/mydataset/tables");

        var result = BigQueryOperationClassifier.Classify(request);

        Assert.Equal(BigQueryOperation.List, result.Operation);
        Assert.Equal("table", result.ResourceType);
        Assert.Equal("mydataset", result.DatasetId);
    }

    [Fact]
    public void Classify_PostTables_ReturnsCreate()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/datasets/mydataset/tables");

        var result = BigQueryOperationClassifier.Classify(request);

        Assert.Equal(BigQueryOperation.Create, result.Operation);
        Assert.Equal("table", result.ResourceType);
        Assert.Equal("mydataset", result.DatasetId);
    }

    [Fact]
    public void Classify_GetTable_ReturnsRead()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/datasets/mydataset/tables/mytable");

        var result = BigQueryOperationClassifier.Classify(request);

        Assert.Equal(BigQueryOperation.Read, result.Operation);
        Assert.Equal("table", result.ResourceType);
        Assert.Equal("mytable", result.ResourceName);
        Assert.Equal("mydataset", result.DatasetId);
    }

    [Fact]
    public void Classify_DeleteTable_ReturnsDelete()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/datasets/mydataset/tables/mytable");

        var result = BigQueryOperationClassifier.Classify(request);

        Assert.Equal(BigQueryOperation.Delete, result.Operation);
        Assert.Equal("table", result.ResourceType);
        Assert.Equal("mytable", result.ResourceName);
    }

    [Fact]
    public void Classify_PatchTable_ReturnsUpdate()
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"{BaseUrl}/datasets/mydataset/tables/mytable");

        var result = BigQueryOperationClassifier.Classify(request);

        Assert.Equal(BigQueryOperation.Update, result.Operation);
        Assert.Equal("table", result.ResourceType);
        Assert.Equal("mytable", result.ResourceName);
    }

    [Fact]
    public void Classify_PutTable_ReturnsUpdate()
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"{BaseUrl}/datasets/mydataset/tables/mytable");

        var result = BigQueryOperationClassifier.Classify(request);

        Assert.Equal(BigQueryOperation.Update, result.Operation);
        Assert.Equal("table", result.ResourceType);
    }

    // ──────────────────────────────────────────────────────────
    //  Streaming insert
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void Classify_PostInsertAll_ReturnsInsert()
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            $"{BaseUrl}/datasets/mydataset/tables/mytable/insertAll");

        var result = BigQueryOperationClassifier.Classify(request);

        Assert.Equal(BigQueryOperation.Insert, result.Operation);
        Assert.Equal("table", result.ResourceType);
        Assert.Equal("mytable", result.ResourceName);
        Assert.Equal("mydataset", result.DatasetId);
    }

    // ──────────────────────────────────────────────────────────
    //  Table data (tabledata.list)
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void Classify_GetTableData_ReturnsList()
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"{BaseUrl}/datasets/mydataset/tables/mytable/data");

        var result = BigQueryOperationClassifier.Classify(request);

        Assert.Equal(BigQueryOperation.List, result.Operation);
        Assert.Equal("tabledata", result.ResourceType);
        Assert.Equal("mytable", result.ResourceName);
    }

    // ──────────────────────────────────────────────────────────
    //  Job operations
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void Classify_GetJobs_ReturnsList()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/jobs");

        var result = BigQueryOperationClassifier.Classify(request);

        Assert.Equal(BigQueryOperation.List, result.Operation);
        Assert.Equal("job", result.ResourceType);
    }

    [Fact]
    public void Classify_PostJobs_ReturnsCreate()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/jobs");

        var result = BigQueryOperationClassifier.Classify(request);

        Assert.Equal(BigQueryOperation.Create, result.Operation);
        Assert.Equal("job", result.ResourceType);
    }

    [Fact]
    public void Classify_GetJob_ReturnsRead()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/jobs/job456");

        var result = BigQueryOperationClassifier.Classify(request);

        Assert.Equal(BigQueryOperation.Read, result.Operation);
        Assert.Equal("job", result.ResourceType);
        Assert.Equal("job456", result.ResourceName);
    }

    [Fact]
    public void Classify_DeleteJob_ReturnsDelete()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/jobs/job456/delete");

        var result = BigQueryOperationClassifier.Classify(request);

        Assert.Equal(BigQueryOperation.Delete, result.Operation);
        Assert.Equal("job", result.ResourceType);
        Assert.Equal("job456", result.ResourceName);
    }

    [Fact]
    public void Classify_PostCancel_ReturnsCancel()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/jobs/job456/cancel");

        var result = BigQueryOperationClassifier.Classify(request);

        Assert.Equal(BigQueryOperation.Cancel, result.Operation);
        Assert.Equal("job", result.ResourceType);
        Assert.Equal("job456", result.ResourceName);
    }

    // ──────────────────────────────────────────────────────────
    //  Model operations
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void Classify_GetModels_ReturnsList()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/datasets/mydataset/models");

        var result = BigQueryOperationClassifier.Classify(request);

        Assert.Equal(BigQueryOperation.List, result.Operation);
        Assert.Equal("model", result.ResourceType);
    }

    [Fact]
    public void Classify_GetModel_ReturnsRead()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/datasets/mydataset/models/mymodel");

        var result = BigQueryOperationClassifier.Classify(request);

        Assert.Equal(BigQueryOperation.Read, result.Operation);
        Assert.Equal("model", result.ResourceType);
        Assert.Equal("mymodel", result.ResourceName);
    }

    [Fact]
    public void Classify_DeleteModel_ReturnsDelete()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/datasets/mydataset/models/mymodel");

        var result = BigQueryOperationClassifier.Classify(request);

        Assert.Equal(BigQueryOperation.Delete, result.Operation);
        Assert.Equal("model", result.ResourceType);
        Assert.Equal("mymodel", result.ResourceName);
    }

    [Fact]
    public void Classify_PatchModel_ReturnsUpdate()
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"{BaseUrl}/datasets/mydataset/models/mymodel");

        var result = BigQueryOperationClassifier.Classify(request);

        Assert.Equal(BigQueryOperation.Update, result.Operation);
        Assert.Equal("model", result.ResourceType);
        Assert.Equal("mymodel", result.ResourceName);
    }

    // ──────────────────────────────────────────────────────────
    //  Routine operations
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void Classify_GetRoutines_ReturnsList()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/datasets/mydataset/routines");

        var result = BigQueryOperationClassifier.Classify(request);

        Assert.Equal(BigQueryOperation.List, result.Operation);
        Assert.Equal("routine", result.ResourceType);
    }

    [Fact]
    public void Classify_GetRoutine_ReturnsRead()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/datasets/mydataset/routines/myroutine");

        var result = BigQueryOperationClassifier.Classify(request);

        Assert.Equal(BigQueryOperation.Read, result.Operation);
        Assert.Equal("routine", result.ResourceType);
        Assert.Equal("myroutine", result.ResourceName);
    }

    [Fact]
    public void Classify_DeleteRoutine_ReturnsDelete()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete,
            $"{BaseUrl}/datasets/mydataset/routines/myroutine");

        var result = BigQueryOperationClassifier.Classify(request);

        Assert.Equal(BigQueryOperation.Delete, result.Operation);
        Assert.Equal("routine", result.ResourceType);
    }

    [Fact]
    public void Classify_PutRoutine_ReturnsUpdate()
    {
        var request = new HttpRequestMessage(HttpMethod.Put,
            $"{BaseUrl}/datasets/mydataset/routines/myroutine");

        var result = BigQueryOperationClassifier.Classify(request);

        Assert.Equal(BigQueryOperation.Update, result.Operation);
        Assert.Equal("routine", result.ResourceType);
    }

    // ──────────────────────────────────────────────────────────
    //  Upload endpoint variation
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void Classify_UploadPostJobs_ReturnsCreate()
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            "https://bigquery.googleapis.com/upload/bigquery/v2/projects/my-project/jobs");

        var result = BigQueryOperationClassifier.Classify(request);

        Assert.Equal(BigQueryOperation.Create, result.Operation);
        Assert.Equal("job", result.ResourceType);
        Assert.Equal("my-project", result.ProjectId);
    }

    // ──────────────────────────────────────────────────────────
    //  Query string parameters don't interfere
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void Classify_WithQueryParams_StillClassifiesCorrectly()
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"{BaseUrl}/datasets/mydataset/tables?maxResults=100&pageToken=abc");

        var result = BigQueryOperationClassifier.Classify(request);

        Assert.Equal(BigQueryOperation.List, result.Operation);
        Assert.Equal("table", result.ResourceType);
    }

    // ──────────────────────────────────────────────────────────
    //  Edge case: Unrecognised path
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void Classify_UnrecognisedPath_ReturnsOther()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://bigquery.googleapis.com/discovery/v1/apis");

        var result = BigQueryOperationClassifier.Classify(request);

        Assert.Equal(BigQueryOperation.Other, result.Operation);
    }

    [Fact]
    public void Classify_NullRequestUri_ReturnsOther()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, (Uri?)null);

        var result = BigQueryOperationClassifier.Classify(request);

        Assert.Equal(BigQueryOperation.Other, result.Operation);
    }

    // ──────────────────────────────────────────────────────────
    //  Diagram labels
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void GetDiagramLabel_Detailed_ReturnsOperationName()
    {
        var info = new BigQueryOperationInfo(BigQueryOperation.Query, "query", "job123", "my-project", null);

        var label = BigQueryOperationClassifier.GetDiagramLabel(info, BigQueryTrackingVerbosity.Detailed);

        Assert.Equal("Query", label);
    }

    [Fact]
    public void GetDiagramLabel_Summarised_ReturnsOperationName()
    {
        var info = new BigQueryOperationInfo(BigQueryOperation.Insert, "table", "mytable", "my-project", "mydataset");

        var label = BigQueryOperationClassifier.GetDiagramLabel(info, BigQueryTrackingVerbosity.Summarised);

        Assert.Equal("Insert", label);
    }

    [Fact]
    public void GetDiagramLabel_Raw_ReturnsNull()
    {
        var info = new BigQueryOperationInfo(BigQueryOperation.Read, "table", "mytable", "my-project", "mydataset");

        var label = BigQueryOperationClassifier.GetDiagramLabel(info, BigQueryTrackingVerbosity.Raw);

        Assert.Null(label);
    }
}
