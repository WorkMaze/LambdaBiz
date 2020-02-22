# LambdaBiz

**Long running stateful orchestrations in C# using AWS Lambda.**
AWS lambda enables users to write serverless functions. However, a lambda function can have a maximum execution time of 15 minutes after which it times out. Hence, it is not possible to write long running processes in AWS lambda. AWS has introduced step functions to overcome this shortcomming. However, there is a steep learning curve to learn the state machine language and the service itself comes at a premium cost.

The purpose of this project is to enable existing C# users of AWS to write long running orchestrations which are durable. 
