namespace AWS.Messaging.Serialization;

/// <summary>
/// Represents metadata associated with a message, including SQS, SNS, and EventBridge specific information.
/// </summary>
internal class MessageMetadata
{
    /// <summary>
    /// Gets or sets the SQS-specific metadata.
    /// </summary>
    public SQSMetadata? SQSMetadata { get; set; }

    /// <summary>
    /// Gets or sets the SNS-specific metadata.
    /// </summary>
    public SNSMetadata? SNSMetadata { get; set; }

    /// <summary>
    /// Gets or sets the EventBridge-specific metadata.
    /// </summary>
    public EventBridgeMetadata? EventBridgeMetadata { get; set; }

    /// <summary>
    /// Initializes a new instance of the MessageMetadata class.
    /// </summary>
    public MessageMetadata()
    {
    }

    /// <summary>
    /// Initializes a new instance of the MessageMetadata class with specified metadata.
    /// </summary>
    /// <param name="sqsMetadata">The SQS metadata.</param>
    /// <param name="snsMetadata">The SNS metadata.</param>
    /// <param name="eventBridgeMetadata">The EventBridge metadata.</param>
    public MessageMetadata(
        SQSMetadata? sqsMetadata = null,
        SNSMetadata? snsMetadata = null,
        EventBridgeMetadata? eventBridgeMetadata = null)
    {
        SQSMetadata = sqsMetadata;
        SNSMetadata = snsMetadata;
        EventBridgeMetadata = eventBridgeMetadata;
    }
}
