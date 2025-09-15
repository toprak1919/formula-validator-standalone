grammar Formula;

options { language = CSharp; }

@header {
#pragma warning disable 3021
}

formula         : expr EOF ;

expr            : cmp ;

cmp             : add ( (GE|LE|EQ|NEQ|GT|LT) add )* ;

add             : mul ( (PLUS|MINUS) mul )* ;

mul             : pow ( (STAR|SLASH|PERCENT) pow )* ;

pow             : unary ( POW unary )* ;

unary           : PLUS unary                         # unaryPlus
                | MINUS unary                        # unaryMinus
                | primary                            # unaryPrimary
                ;

primary         : NUMBER                             # numberPrimary
                | varRef                             # varPrimary
                | constRef                           # constPrimary
                | funcCall                           # funcPrimary
                | LPAREN expr RPAREN                 # parenPrimary
                ;

varRef          : DOLLAR IDENT (DOT IDENT)? ;
constRef        : HASH IDENT ;
funcCall        : IDENT LPAREN (expr (COMMA expr)*)? RPAREN ;

NUMBER          : DIGIT+ ('.' DIGIT+)? ( [eE] [+-]? DIGIT+ )? ;
IDENT           : [a-zA-Z_][a-zA-Z0-9_]* ;

PLUS            : '+';
MINUS           : '-';
STAR            : '*';
SLASH           : '/';
PERCENT         : '%';
POW             : '^';

GE              : '>=';
LE              : '<=';
EQ              : '==';
NEQ             : '!=';
GT              : '>';
LT              : '<';

LPAREN          : '(';
RPAREN          : ')';
COMMA           : ',';
DOT             : '.';
DOLLAR          : '$';
HASH            : '#';

WS              : [ \t\r\n]+ -> skip ;

fragment DIGIT  : [0-9] ;

