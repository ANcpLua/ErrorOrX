; Unshipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

 Rule ID | Category          | Severity | Notes                                
---------|-------------------|----------|--------------------------------------
 EOE001  | ErrorOr.Endpoints | Error    | Invalid return type                  
 EOE002  | ErrorOr.Endpoints | Error    | Handler must be static               
 EOE003  | ErrorOr.Endpoints | Error    | Route parameter not bound            
 EOE004  | ErrorOr.Endpoints | Error    | Duplicate route                      
 EOE005  | ErrorOr.Endpoints | Error    | Invalid route pattern                
 EOE006  | ErrorOr.Endpoints | Error    | Multiple body sources                
 EOE007  | ErrorOr.Endpoints | Warning  | Type not AOT-serializable            
 EOE008  | ErrorOr.Endpoints | Warning  | Undocumented custom error            
 EOE009  | ErrorOr.Endpoints | Warning  | Body on read-only HTTP method        
 EOE010  | ErrorOr.Endpoints | Warning  | AcceptedResponse on read-only method 
 EOE011  | ErrorOr.Endpoints | Error    | Invalid FromRoute type               
 EOE012  | ErrorOr.Endpoints | Error    | Invalid FromQuery type               
 EOE013  | ErrorOr.Endpoints | Error    | Invalid AsParameters type            
 EOE014  | ErrorOr.Endpoints | Error    | AsParameters type has no constructor 
 EOE015  | ErrorOr.Endpoints | Warning  | Non-nullable binding parameter       
 EOE016  | ErrorOr.Endpoints | Error    | Invalid FromHeader type              
 EOE020  | ErrorOr.Endpoints | Error    | Conflicting route constraints        
 EOE021  | ErrorOr.Endpoints | Warning  | Unknown route constraint             
 EOE022  | ErrorOr.Endpoints | Warning  | Optional route parameter not at end  
 EOE023  | ErrorOr.Endpoints | Warning  | Route constraint type mismatch       
 EOE024  | ErrorOr.Endpoints | Error    | Catch-all must be string
 EOE030  | ErrorOr.Endpoints | Info     | Too many result types                
 EOE031  | ErrorOr.Endpoints | Warning  | ProducesError status mismatch        
 EOE032  | ErrorOr.Endpoints | Warning  | Unknown error factory                
 EOE033  | ErrorOr.Endpoints | Error    | Undocumented interface call          
 EOE040  | ErrorOr.Endpoints | Info     | SSE error handling limitation        
 EOE041  | ErrorOr.Endpoints | Warning  | SSE with request body                