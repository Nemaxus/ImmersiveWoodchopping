﻿using System;
using System.Reflection.Metadata;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace ImmersiveWoodchopping
{
    public class WoodChopping : CollectibleBehavior
    {
        public WoodChopping(CollectibleObject collObj) : base(collObj)
        {
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
        {
            //base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling);
            IPlayer byPlayer = (byEntity as EntityPlayer).Player;
            if (blockSel != null)
            {
                Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
                if (IsChoppable(block))
                {
                    byPlayer.Entity.StartAnimation("immersiveaxechopvertical");
                    handHandling = EnumHandHandling.PreventDefault;
                    byEntity.WatchedAttributes.SetBool(Constants.ModId + ":madeaswing", false);
                    byEntity.WatchedAttributes.SetBool(Constants.ModId + ":haschoppedblock", false);
                }
            }
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
        {
            handling = EnumHandling.PreventDefault;
            IPlayer byPlayer = (byEntity as EntityPlayer).Player;
            IWorldAccessor world = byEntity.World;
            IBlockAccessor blockAccessor = world.BlockAccessor;


            if (blockSel != null)
            {
                Block block = blockAccessor.GetBlock(blockSel.Position);
                if (IsChoppable(block))
                {
                    if (world.Side == EnumAppSide.Client)
                    {
                        //AnimationStep(secondsUsed, byEntity);

                        //AnimationStepFreeMouse(byEntity);

                        if (secondsUsed > 0.4f && byEntity.WatchedAttributes.GetBool(Constants.ModId + ":madeaswing") == false)
                        {
                            var pitch = (byEntity as EntityPlayer).talkUtil.pitchModifier;

                            world.PlaySoundAt(new AssetLocation("sounds/block/chop"), byPlayer.Entity, byPlayer, pitch * 0.9f + (float)world.Rand.NextDouble() * 0.2f, 16, 1f);

                            (world as IClientWorldAccessor)?.AddCameraShake(0.25f);
                            byEntity.WatchedAttributes.SetBool(Constants.ModId + ":madeaswing", true);
                            byEntity.WatchedAttributes.SetBool(Constants.ModId + ":haschoppedblock", true);
                            //Crappy way to fix animation bug T_T
                            TryStopAnimation(byEntity, byPlayer);
                        }
                    }


                    if (world.Side == EnumAppSide.Server)
                    {
                        if (secondsUsed > 0.45f && !byEntity.WatchedAttributes.GetBool(Constants.ModId + ":haschoppedblock"))
                        {
                            int minToolTier = world.Config.GetInt(Constants.ModId + ":IntsaChopMinTier");

                            Item item = byEntity.RightHandItemSlot.Itemstack.Item;
                            float chopChance = item.ToolTier / (float)(minToolTier == 0 ? 1 : minToolTier);
                            if (world.Rand.NextDouble() > chopChance)
                            {
                                if (world.Config.TryGetBool(Constants.ModId + ":DamageToolOnChop") == true)
                                {
                                    item.DamageItem(world, byEntity, byEntity.RightHandItemSlot);
                                    //blockAccessor.DamageBlock(blockSel.Position, BlockFacing.FromNormal(byEntity.Pos.GetViewVector()), block.Resistance * chopChance);
                                }
                                blockAccessor.DamageBlock(blockSel.Position, BlockFacing.FromNormal(byEntity.Pos.GetViewVector()), block.Resistance * chopChance);
                            }
                            else
                            {
                                BlockBehaviorAxeChoppable blockBehavior = block.GetBehavior(typeof(BlockBehaviorAxeChoppable), true) as BlockBehaviorAxeChoppable;

                                blockAccessor.BreakBlock(blockSel.Position, byPlayer, 0);
                                blockAccessor.MarkBlockDirty(blockSel.Position, byPlayer);
                                Item drops = world.GetItem(blockBehavior.drop);

                                for (int i = 0; i < blockBehavior.dropAmount; i++)
                                {
                                    //world.SpawnItemEntity(new ItemStack(drops, 1), blockSel.Position.ToVec3d());
                                }
                                item.DamageItem(world, byEntity, byEntity.RightHandItemSlot);

                                if (world.Config.TryGetBool(Constants.ModId + ":AutoLogPlacement") == true)
                                {
                                    PlaceNextLog(byEntity, byPlayer, blockAccessor, blockSel);
                                }
                            }
                            byEntity.WatchedAttributes.SetBool(Constants.ModId + ":haschoppedblock", true);
                        }
                    }
                }
            }
            return secondsUsed < 0.75f;
        }

        private void TryStopAnimation(EntityAgent byEntity, IPlayer byPlayer)
        {
            if (byEntity.RightHandItemSlot.Itemstack.Item.GetRemainingDurability(byEntity.RightHandItemSlot.Itemstack) <= 1)
            {
                byPlayer.Entity.StopAnimation("immersiveaxechopvertical");
            }
        }

        public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason, ref EnumHandling handled)
        {
            IPlayer byPlayer = (byEntity as EntityPlayer).Player;
            byPlayer.Entity.StopAnimation("immersiveaxechopvertical");
            handled = EnumHandling.PreventDefault;
            return true;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
        {
            IPlayer byPlayer = (byEntity as EntityPlayer).Player;
            byPlayer.Entity.StopAnimation("immersiveaxechopvertical");
            handling = EnumHandling.PreventDefault;

        }
        private void PlaceNextLog(EntityAgent byEntity, IPlayer byPlayer, IBlockAccessor blockAccessor, BlockSelection blockSel)
        {
            Block foungLog = null;
            ItemSlot logInSlot = null;
            byEntity.WalkInventory((currentSlot) =>
            {
                if (currentSlot is ItemSlotCreative) return true;
                if (!(currentSlot.Inventory is InventoryBasePlayer)) return true;
                Block blockInCurrentSlot = currentSlot.Itemstack?.Block;
                if (blockInCurrentSlot != null && IsChoppable(blockInCurrentSlot))
                {
                    foungLog = blockInCurrentSlot;
                    logInSlot = currentSlot;
                    return false;
                }
                return true;
            });
            if (foungLog != null)
            {
                blockAccessor.SetBlock(foungLog.Id, blockSel.Position);
                byEntity.World.PlaySoundAt(foungLog.Sounds.Place, byPlayer);
                if(byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
                {
                logInSlot.TakeOut(1);
                logInSlot.MarkDirty();
                }
            }
        }

        private bool IsChoppable(Block block)
        {
            return block.HasBehavior<BlockBehaviorAxeChoppable>();
        }
/*
        private void AnimationStepFreeMouse(EntityAgent byEntity)
        {
            var mouseY = (byEntity.Api as ICoreClientAPI).Input.MouseY;
            var tf = new ModelTransform();
            tf.EnsureDefaultValues();
            tf.Translation.Set(0, mouseY/500, 0);
            byEntity.Controls.UsingHeldItemTransformAfter = tf;
        }
        private void AnimationStep(float secondsUsed, EntityAgent byEntity)
        {
            float t = secondsUsed * 1f;
            float backwards = -Math.Min(0.35f, 2 * t);
            float stab = Math.Min(1.2f, 20 * Math.Max(0, t - 0.35f));


            var tf = new ModelTransform();
            tf.EnsureDefaultValues();

            float sum = stab + backwards;
            float easeout = Math.Max(0, 2 * (t - 1));

            if (t > 0.4f) sum = Math.Max(0, sum - easeout);

            tf.Translation.Set(-1.4f * sum, sum, -sum * 0.8f * 2.6f);
            tf.Rotation.Set(-sum * 45, 0, sum * 10);

            byEntity.Controls.UsingHeldItemTransformAfter = tf;

        }
*/

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot, ref EnumHandling handling)
        {
            return new WorldInteraction[]
            {
                new WorldInteraction
                {
                    ActionLangCode = Constants.ModId + ":heldhelp-chop",
                    MouseButton = EnumMouseButton.Right
                }
            };
        }
    }
}