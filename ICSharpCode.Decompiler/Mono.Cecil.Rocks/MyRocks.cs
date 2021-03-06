﻿/*
 * Created by SharpDevelop.
 * User: User
 * Date: 05/02/2011
 * Time: 10:10
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Decompiler.Rocks
{
	static class MyRocks
	{
		static public TypeReference TypeVoid = GetCecilType(typeof(void));
		static public TypeReference TypeObject = GetCecilType(typeof(Object));
		static public TypeReference TypeException = GetCecilType(typeof(Exception));
		static public TypeReference TypeBool = GetCecilType(typeof(bool));
		static public TypeReference TypeInt32 = GetCecilType(typeof(Int32));
		static public TypeReference TypeString = GetCecilType(typeof(string));
		static public TypeReference TypeZero = GetCecilType(typeof(Int32));
		static public TypeReference TypeOne = GetCecilType(typeof(Int32));
		
		public static List<T> CutRange<T>(this List<T> list, int start, int count)
		{
			List<T> ret = new List<T>(count);
			for (int i = 0; i < count; i++) {
				ret.Add(list[start + i]);
			}
			list.RemoveRange(start, count);
			return ret;
		}
		
		public static bool CanFallThough(this OpCode opCode)
		{
			switch(opCode.FlowControl) {
				case FlowControl.Branch:			return false;
				case FlowControl.Cond_Branch:	return true;
				case FlowControl.Next:			return true;
				case FlowControl.Call:			return true;
				case FlowControl.Return:			return false;
				case FlowControl.Throw:			return false;
				case FlowControl.Meta:			return true;
				default: throw new NotImplementedException();
			}
		}
		
		public static bool IsBranch(this OpCode opCode)
		{
			return opCode.FlowControl == FlowControl.Branch || opCode.FlowControl == FlowControl.Cond_Branch;
		}
		
		public static int? GetPopCount(this Instruction inst)
		{
			switch(inst.OpCode.StackBehaviourPop) {
				case StackBehaviour.Pop0:   				return 0;
				case StackBehaviour.Pop1:   				return 1;
				case StackBehaviour.Popi:   				return 1;
				case StackBehaviour.Popref: 				return 1;
				case StackBehaviour.Pop1_pop1:   		return 2;
				case StackBehaviour.Popi_pop1:   		return 2;
				case StackBehaviour.Popi_popi:   		return 2;
				case StackBehaviour.Popi_popi8:  		return 2;
				case StackBehaviour.Popi_popr4:  		return 2;
				case StackBehaviour.Popi_popr8:  		return 2;
				case StackBehaviour.Popref_pop1: 		return 2;
				case StackBehaviour.Popref_popi: 		return 2;
				case StackBehaviour.Popi_popi_popi:     return 3;
				case StackBehaviour.Popref_popi_popi:   return 3;
				case StackBehaviour.Popref_popi_popi8:  return 3;
				case StackBehaviour.Popref_popi_popr4:  return 3;
				case StackBehaviour.Popref_popi_popr8:  return 3;
				case StackBehaviour.Popref_popi_popref: return 3;
				case StackBehaviour.PopAll: 				return null;
				case StackBehaviour.Varpop: 
					switch(inst.OpCode.Code) {
						case Code.Call:
						case Code.Callvirt:
							MethodReference cecilMethod = ((MethodReference)inst.Operand);
							if (cecilMethod.HasThis) {
								return cecilMethod.Parameters.Count + 1 /* this */;
							} else {
								return cecilMethod.Parameters.Count;
							}
						case Code.Calli:    throw new NotImplementedException();
						case Code.Ret:		return null;
						case Code.Newobj:
							MethodReference ctorMethod = ((MethodReference)inst.Operand);
							return ctorMethod.Parameters.Count;
						default: throw new Exception("Unknown Varpop opcode");
					}
				default: throw new Exception("Unknown pop behaviour: " + inst.OpCode.StackBehaviourPop);
			}
		}
		
		public static int GetPushCount(this Instruction inst)
		{
			switch(inst.OpCode.StackBehaviourPush) {
				case StackBehaviour.Push0:       return 0;
				case StackBehaviour.Push1:       return 1;
				case StackBehaviour.Push1_push1: return 2;
				case StackBehaviour.Pushi:       return 1;
				case StackBehaviour.Pushi8:      return 1;
				case StackBehaviour.Pushr4:      return 1;
				case StackBehaviour.Pushr8:      return 1;
				case StackBehaviour.Pushref:     return 1;
				case StackBehaviour.Varpush:     // Happens only for calls
					switch(inst.OpCode.Code) {
						case Code.Call:
						case Code.Callvirt:
							MethodReference cecilMethod = ((MethodReference)inst.Operand);
							if (cecilMethod.ReturnType.FullName == Constants.Void) {
								return 0;
							} else {
								return 1;
							}
						case Code.Calli:    throw new NotImplementedException();
						default: throw new Exception("Unknown Varpush opcode");
					}
				default: throw new Exception("Unknown push behaviour: " + inst.OpCode.StackBehaviourPush);
			}
		}
		
		static public TypeReference GetCecilType(Type type)
		{
			return new TypeReference(type.Name, type.Namespace, null, null, type.IsValueType);
		}
		
		static public TypeReference GetTypeInternal(this Instruction inst, MethodDefinition methodDef, List<TypeReference> args)
		{
			OpCode opCode = inst.OpCode;
			object operand = inst.Operand;
			TypeReference operandAsTypeRef = operand as TypeReference;
			//ByteCode operandAsByteCode = operand as ByteCode;
			//string operandAsByteCodeLabel = operand is ByteCode ? String.Format("IL_{0:X2}", ((ByteCode)operand).Offset) : null;
			TypeReference arg1 = args.Count >= 1 ? args[0] : null;
			TypeReference arg2 = args.Count >= 2 ? args[1] : null;
			TypeReference arg3 = args.Count >= 3 ? args[2] : null;
			
			switch(opCode.Code) {
				#region Arithmetic
					case Code.Add:        
					case Code.Add_Ovf:    
					case Code.Add_Ovf_Un: 
					case Code.Div:        
					case Code.Div_Un:     
					case Code.Mul:        
					case Code.Mul_Ovf:    
					case Code.Mul_Ovf_Un: 
					case Code.Rem:        
					case Code.Rem_Un:     
					case Code.Sub:        
					case Code.Sub_Ovf:    
					case Code.Sub_Ovf_Un: 
					case Code.And:        
					case Code.Xor:        
					case Code.Shl:        
					case Code.Shr:        
					case Code.Shr_Un:     return TypeInt32;
					
					case Code.Neg:        return TypeInt32;
					case Code.Not:        return TypeInt32;
				#endregion
				#region Arrays
					case Code.Newarr:
						return new ArrayType(operandAsTypeRef);
					
					case Code.Ldlen: return TypeInt32;
					
					case Code.Ldelem_I:   
					case Code.Ldelem_I1:  
					case Code.Ldelem_I2:  
					case Code.Ldelem_I4:  
					case Code.Ldelem_I8:  return TypeInt32;
					case Code.Ldelem_U1:  
					case Code.Ldelem_U2:  
					case Code.Ldelem_U4:  
					case Code.Ldelem_R4:  
					case Code.Ldelem_R8:  throw new NotImplementedException();
					case Code.Ldelem_Ref: 
						if (arg1 is ArrayType) {
							return ((ArrayType)arg1).ElementType;
						} else {
							throw new NotImplementedException();
						}
					case Code.Ldelem_Any: 
					case Code.Ldelema:    throw new NotImplementedException();
					
					case Code.Stelem_I:   
					case Code.Stelem_I1:  
					case Code.Stelem_I2:  
					case Code.Stelem_I4:  
					case Code.Stelem_I8:  
					case Code.Stelem_R4:  
					case Code.Stelem_R8:  
					case Code.Stelem_Ref: 
					case Code.Stelem_Any: return TypeVoid;
				#endregion
				#region Branching
					case Code.Br:      
					case Code.Brfalse: 
					case Code.Brtrue:  
					case Code.Beq:     
					case Code.Bge:     
					case Code.Bge_Un:  
					case Code.Bgt:     
					case Code.Bgt_Un:  
					case Code.Ble:     
					case Code.Ble_Un:  
					case Code.Blt:     
					case Code.Blt_Un:  
					case Code.Bne_Un:  return TypeVoid;
				#endregion
				#region Comparison
					case Code.Ceq:    
					case Code.Cgt:    
					case Code.Cgt_Un: 
					case Code.Clt:    
					case Code.Clt_Un: return TypeBool;
				#endregion
				#region Conversions
					case Code.Conv_I:    
					case Code.Conv_I1:   
					case Code.Conv_I2:   
					case Code.Conv_I4:   
					case Code.Conv_I8:   
					case Code.Conv_U:    
					case Code.Conv_U1:   
					case Code.Conv_U2:   
					case Code.Conv_U4:   
					case Code.Conv_U8:   
					case Code.Conv_R4:   
					case Code.Conv_R8:   
					case Code.Conv_R_Un: 
					
					case Code.Conv_Ovf_I:  
					case Code.Conv_Ovf_I1: 
					case Code.Conv_Ovf_I2: 
					case Code.Conv_Ovf_I4: 
					case Code.Conv_Ovf_I8: 
					case Code.Conv_Ovf_U:  
					case Code.Conv_Ovf_U1: 
					case Code.Conv_Ovf_U2: 
					case Code.Conv_Ovf_U4: 
					case Code.Conv_Ovf_U8: 
					
					case Code.Conv_Ovf_I_Un:  
					case Code.Conv_Ovf_I1_Un: 
					case Code.Conv_Ovf_I2_Un: 
					case Code.Conv_Ovf_I4_Un: 
					case Code.Conv_Ovf_I8_Un: 
					case Code.Conv_Ovf_U_Un:  
					case Code.Conv_Ovf_U1_Un: 
					case Code.Conv_Ovf_U2_Un: 
					case Code.Conv_Ovf_U4_Un: 
					case Code.Conv_Ovf_U8_Un: return TypeInt32;
				#endregion
				#region Indirect
					case Code.Ldind_I: throw new NotImplementedException();
					case Code.Ldind_I1: throw new NotImplementedException();
					case Code.Ldind_I2: throw new NotImplementedException();
					case Code.Ldind_I4: throw new NotImplementedException();
					case Code.Ldind_I8: throw new NotImplementedException();
					case Code.Ldind_U1: throw new NotImplementedException();
					case Code.Ldind_U2: throw new NotImplementedException();
					case Code.Ldind_U4: throw new NotImplementedException();
					case Code.Ldind_R4: throw new NotImplementedException();
					case Code.Ldind_R8: throw new NotImplementedException();
					case Code.Ldind_Ref: throw new NotImplementedException();
					
					case Code.Stind_I: throw new NotImplementedException();
					case Code.Stind_I1: throw new NotImplementedException();
					case Code.Stind_I2: throw new NotImplementedException();
					case Code.Stind_I4: throw new NotImplementedException();
					case Code.Stind_I8: throw new NotImplementedException();
					case Code.Stind_R4: throw new NotImplementedException();
					case Code.Stind_R8: throw new NotImplementedException();
					case Code.Stind_Ref: throw new NotImplementedException();
				#endregion
				case Code.Arglist: throw new NotImplementedException();
				case Code.Box: throw new NotImplementedException();
				case Code.Break: throw new NotImplementedException();
				case Code.Call: return ((MethodReference)operand).ReturnType;
				case Code.Calli: throw new NotImplementedException();
				case Code.Callvirt: return ((MethodReference)operand).ReturnType;
				case Code.Castclass: throw new NotImplementedException();
				case Code.Ckfinite: throw new NotImplementedException();
				case Code.Constrained: throw new NotImplementedException();
				case Code.Cpblk: throw new NotImplementedException();
				case Code.Cpobj: throw new NotImplementedException();
				case Code.Dup: throw new NotImplementedException();
				case Code.Endfilter: throw new NotImplementedException();
				case Code.Endfinally: throw new NotImplementedException();
				case Code.Initblk: throw new NotImplementedException();
				case Code.Initobj: throw new NotImplementedException();
				case Code.Isinst: throw new NotImplementedException();
				case Code.Jmp: throw new NotImplementedException();
				case Code.Ldarg:
					TypeReference typeRef = ((ParameterDefinition)operand).ParameterType;
					// 'this' returns null;  TODO: Return proper type of this
					return typeRef ?? TypeObject;
				case Code.Ldarga: throw new NotImplementedException();
				case Code.Ldc_I4:
					if ((int)operand == 0) {
						return TypeZero;
					} else if ((int)operand == 1) {
						return TypeOne;
					} else {
						return TypeInt32;
					}
				case Code.Ldc_I8: throw new NotImplementedException();
				case Code.Ldc_R4: throw new NotImplementedException();
				case Code.Ldc_R8: throw new NotImplementedException();
				case Code.Ldfld: return ((FieldDefinition)operand).FieldType;
				case Code.Ldflda: throw new NotImplementedException();
				case Code.Ldftn: throw new NotImplementedException();
				case Code.Ldloc: return ((VariableDefinition)operand).VariableType;
				case Code.Ldloca: throw new NotImplementedException();
				case Code.Ldnull: throw new NotImplementedException();
				case Code.Ldobj: throw new NotImplementedException();
				case Code.Ldsfld: throw new NotImplementedException();
				case Code.Ldsflda: throw new NotImplementedException();
				case Code.Ldstr: return TypeString;
				case Code.Ldtoken: throw new NotImplementedException();
				case Code.Ldvirtftn: throw new NotImplementedException();
				case Code.Leave: throw new NotImplementedException();
				case Code.Localloc: throw new NotImplementedException();
				case Code.Mkrefany: throw new NotImplementedException();
				case Code.Newobj: throw new NotImplementedException();
				case Code.No: throw new NotImplementedException();
				case Code.Nop: return TypeVoid;
				case Code.Or: throw new NotImplementedException();
				case Code.Pop: throw new NotImplementedException();
				case Code.Readonly: throw new NotImplementedException();
				case Code.Refanytype: throw new NotImplementedException();
				case Code.Refanyval: throw new NotImplementedException();
				case Code.Ret: return TypeVoid;
				case Code.Rethrow: throw new NotImplementedException();
				case Code.Sizeof: throw new NotImplementedException();
				case Code.Starg: throw new NotImplementedException();
				case Code.Stfld: throw new NotImplementedException();
				case Code.Stloc: return TypeVoid;
				case Code.Stobj: throw new NotImplementedException();
				case Code.Stsfld: throw new NotImplementedException();
				case Code.Switch: throw new NotImplementedException();
				case Code.Tail: throw new NotImplementedException();
				case Code.Throw: throw new NotImplementedException();
				case Code.Unaligned: throw new NotImplementedException();
				case Code.Unbox: throw new NotImplementedException();
				case Code.Unbox_Any: throw new NotImplementedException();
				case Code.Volatile: throw new NotImplementedException();
				default: throw new Exception("Unknown OpCode: " + opCode);
			}
		}
	}
}
